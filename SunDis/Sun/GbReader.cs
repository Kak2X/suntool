using System.Runtime.CompilerServices;
using SunCommon;
namespace SunDis;

public record GbReaderResult(PointerTable Playlist, SortedSet<GbData> Sorted);
public class GbReader
{
    private readonly Stream S;
    private readonly int SongCount;
    private readonly GbPtr? BankTablePtr;
    private readonly FormatOptions Opt;
    private readonly SortedSet<GbData> UniqueRom; // NOTE: add SndOpcode here, not SndFunc or SndData
    private readonly OpcodeParser Parser;

    public GbReader(Stream s, int songCount, GbPtr? bankTablePtr, FormatOptions opt)
    {
        S = s;
        SongCount = songCount;
        BankTablePtr = bankTablePtr;
        Opt = opt;
        UniqueRom = [];
        Parser = OpcodeParser.Create(S, Opt);
    }

    public GbReaderResult Read()
    {
        // This parses the PointerTable structure, except 
        var ptrTbl = new PointerTable { Mode = Opt.Mode };
        TryAdd(ptrTbl, out _);

        var songPtrs = (IEnumerable<GbPtr>)ReadSongPtrs();

        // In practice, the first song is always skipped.
        var songId = 0;
        if (Opt.SkipFirstSong)
        {
            songId++;
            songPtrs = songPtrs.Skip(1);
        }

        // For each song in the pointer table...
        foreach (var songPtr in songPtrs)
        {
            // If it's unique...
            S.Seek(songPtr.RomAddress, SeekOrigin.Begin);
            if (!TryGetCurValue(out _))
            {
                var songHdr = new SndHeader { Id = songId, Name = $"{songId:X02}" };
                var songHdrGb = new GbData(GbPtrPool.Create(S.Position), songHdr);
                songHdr.ChannelCount = S.ReadByte();
                if (songHdr.ChannelCount > 4 || songHdr.ChannelCount <= 0)
                    Console.WriteLine($"Song {songId.AsHexByte()} at {songHdrGb.Location.ToBankString()} probably points to code.");
                else
                {
                    UniqueRom.Add(songHdrGb);
                    ptrTbl.Songs.Add(songHdr);
                    // For each sound channel header...
                    for (var i = 0; i < songHdr.ChannelCount; i++)
                        ParseSongChCur(songHdr, i == 0);
                }
            }

            songId++;
        }

        // Try to find unused content?
        if (Opt.FindGaps)
        {
            // This scanner goes off proximity by trying to guess based on the last entity
            var last = (GbData)null!;
            var expectedNext = (long?)null;

            foreach (var x in UniqueRom.ToArray())
            {
                if (expectedNext.HasValue && expectedNext != x.Location.RomAddress)
                {
                    var expectedNextPtr = GbPtrPool.Create(expectedNext.Value);
                    S.Seek(expectedNext.Value, SeekOrigin.Begin);

                    // Try to autodetect what this unreferenced block contains
                    if (last.Data is PointerTable)
                    {
                        Console.WriteLine($"Skipped gap between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");
                    }
                    else if (last.Location.Bank != x.Location.Bank) // checking lastLoc just in case the old bank is filled
                    {
                        Console.WriteLine($"Switched banks ({last.Location.Bank:X2} -> {x.Location.Bank:X2}); Skipped gap between {expectedNextPtr.ToBankString()} to {x.Location.ToBankString()}");
                    }
                    else if (last.Data is SndChHeader lastChHeader)
                    {
                        Console.WriteLine($"Unreferenced sound channel between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");
                        // Assume that any unreferenced data right after a sound channel header is also a sound channel header.
                        ParseSongChCur(lastChHeader.Parent!, isUnused: true);
                    }
                    else if (last.Data is SndOpcode lastOpcode)
                    {
                        if (TryGetCurValue(out _))
                            Console.WriteLine($"Likely content of unreferenced sound channel detected between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}, already processed.");
                        else
                        {
                            Console.WriteLine($"Gap detected between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");
                            while (S.Position < x.Location.RomAddress)
                            {
                                //--
                                // Detect where the new function should be inserted.
                                // Assumed it cannot be before Main.
                                var lastFunc = lastOpcode.Parent!;
                                var lastData = lastFunc.Parent!;
                                int slot;
                                if (lastFunc == lastData.Main)
                                    slot = 0;
                                else
                                {
                                    var idx = lastData.Subs.IndexOf(lastFunc);
                                    slot = idx == -1 ? lastData.Subs.Count : idx + 1;
                                }
                                //--

                                var func = new SndFunc { Parent = lastOpcode.Parent.Parent, IsUnused = true };
                                Console.WriteLine($"-> Inserting func at #{slot:X}");
                                func.Parent!.Subs.Insert(slot, func);
                                ParseFuncCur(func);
                            }
                        }
                    } 
                    else
                    {
                        Console.WriteLine($"UNKNOWN Gap detected between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");
                    }
                }

                last = x;
                expectedNext = x.Location.RomAddress + x.Data.SizeInRom();
            }
        }

        // Finally, fill in the target opcodes now that we have the entire tree
        foreach (var x in UniqueRom)
            if (x.Data is IHasPointerEx ptrDat)
            {
                TryGetValue(ptrDat.TargetPtr, out var target);
                ptrDat.Target = (SndOpcode)target.Data!;
            }

        return new GbReaderResult(ptrTbl, UniqueRom);
    }

    private GbPtr[] ReadSongPtrs()
    {
        if (Opt.Mode == DataMode.OPEx)
        {
            // Bankswitching, single struct with bank + addr + init code ptr
            return Enumerable.Range(0, SongCount).Select(i =>
            {
                var res = S.ReadFarPtr();
                S.Seek(2, SeekOrigin.Current); // Skip init code ptr
                return res;
            }).ToArray();
        }
        else
        {
            // Read song pointers
            if (Opt.Mode == DataMode.OP)
            {
                // Bankswitching, separate bank table
                var ptrs = S.ReadUint16s(SongCount);
                // If not specified explicitly, the bank table begins after the song pointer table.
                // In the games I looked at that is always the case
                if (BankTablePtr is not null)
                    S.Seek(BankTablePtr.RomAddress, SeekOrigin.Begin);
                var banks = S.ReadUint8s(SongCount);
                return Enumerable.Range(0, SongCount).Select(i => GbPtrPool.Create(banks[i], ptrs[i])).ToArray();
            }
            else
            {
                // No bankswitching
                return Enumerable.Range(0, SongCount).Select(_ => S.ReadLocalPtr()).ToArray();
            }
        }
    }

    private void ParseSongChCur(SndHeader songHdr, bool isFirstChannel = false, bool isUnused = false)
    {
        // These are always unique, TryAdd will always return true
        var chHeader = new SndChHeader { Parent = songHdr, IsUnused = isUnused, Data = { Main = { IsUnused = isUnused } } };
        if (!TryAdd(chHeader, out _))
            throw new InvalidOperationException("How did this happen? (non-unique SndChHeader)");

        songHdr.Channels.Add(chHeader);

        chHeader.InitialStatus = (SndInfoStatus)S.ReadByte();
        chHeader.SoundChannelPtr = (SndChPtrNum)S.ReadByte();
        var mainPtr = S.ReadLocalPtr();
        chHeader.FineTune = S.ReadByte();
        chHeader.Unused = (byte)S.ReadByte();
        if (isFirstChannel)
        {
            if (songHdr.Id == 0x0C)
                songHdr.Kind = SongKind.Pause;
            else if (songHdr.Id == 0x0D)
                songHdr.Kind = SongKind.Unpause;
            else if (chHeader.InitialStatus.HasFlag(SndInfoStatus.SIS_SFX))
                songHdr.Kind = SongKind.SFX;
            else
                songHdr.Kind = SongKind.BGM;
        }

        // Parse out the sound data
        Seek(mainPtr, () =>
        {
            if (TryGetCurValue(out var existingData))
                chHeader.Data = ((SndOpcode)existingData.Data).Parent.Parent!;
            else
                ParseFuncCur(chHeader.Data.Main);
        });
    }

    private void ParseFuncCur(SndFunc main)
    {
        var songData = main.Parent!;
        // Start with the main function
        if (!TryParseAndAdd(main, out _))
            return;

        // Find calls from main function
        var uniqueCalls = new HashSet<GbPtr>();
        foreach (var x in main.Opcodes.OfType<CmdCallEx>())
            uniqueCalls.Add(x.TargetPtr);

        // Traverse nested subroutines, finding calls as we go.
        // This uses a second buffer to store newly found unique nested calls,
        // which is swapped at the end with the processed ones.
        var subsToAdd = new SortedSet<GbData>(); 
        var toProc = uniqueCalls.ToList();
        var toProcNext = new List<GbPtr>();
        while (true)
        {
            foreach (var call in toProc)
            {
                //--
                S.Seek(call.RomAddress, SeekOrigin.Begin);
                var sub = new SndFunc { Parent = songData, IsUnused = main.IsUnused };
                if (TryParseAndAdd(sub, out var subGb))
                    foreach (var x in sub.Opcodes.OfType<CmdCallEx>())
                        if (uniqueCalls.Add(x.TargetPtr))
                            toProcNext.Add(x.TargetPtr); // "recursion"
                subsToAdd.Add(subGb);
                //--
            }
            // If no more unique subroutines were found, we're done
            if (toProcNext.Count == 0)
                break;
            // Prepare for next round
            (toProc, toProcNext) = (toProcNext, toProc);
            toProcNext.Clear();
        }

        // Add the calls by ROM order
        foreach (var sub in subsToAdd)
            if (sub.Data is SndFunc func)
                songData.Subs.Add(func);
    }

    private bool TryParseAndAdd(SndFunc songFunc, out GbData songFuncGb)
    {
        // Skip already processed instructions
        // It cannot use TryAdd() because SndFunc should not be added to UniqueRom
        songFuncGb = new GbData(GbPtrPool.Create(S.Position), songFunc);
        if (UniqueRom.TryGetValue(songFuncGb, out var existing))
        {
            songFuncGb = existing;
            return false;
        }

        // Read out all opcodes from the subroutine, until a terminator is reached.
        SndOpcode cmd;
        do
        {
            var gbCmd = Parser.Parse(songFunc);
            if (!UniqueRom.Add(gbCmd))
            {
                // We can get here if there's an unused subroutine that falls through used data.
                // If we got here to begin with, however, at least one new opcode was written, so must return true.
                return true;
            }
            cmd = (SndOpcode)gbCmd.Data;
            songFunc.Opcodes.Add(cmd);
        } while (!cmd.Terminates);

        return true;
    }

    // Seeks to the specified point, restoring it when exiting
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Seek(GbPtr addr, Action func)
    {
        var originalPos = S.Position;
        S.Seek(addr.RomAddress, SeekOrigin.Begin);
        func();
        S.Seek(originalPos, SeekOrigin.Begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetCurValue(out GbData existingData)
    {
        return UniqueRom.TryGetValue(new GbData(GbPtrPool.Create(S.Position), null!), out existingData!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetValue(GbPtr ptr, out GbData existingData)
    {
        return UniqueRom.TryGetValue(new GbData(ptr, null!), out existingData!);
    }

    private bool TryAdd(IRomData data, out GbData fullOut)
    {
        var toInsert = new GbData(GbPtrPool.Create(S.Position), data);
        if (UniqueRom.Add(toInsert))
        {
            fullOut = toInsert;
            return true; // newly inserted
        }
        else
        {
            UniqueRom.TryGetValue(toInsert, out var real);
            fullOut = real!;
            return false;
        }
    }
}
