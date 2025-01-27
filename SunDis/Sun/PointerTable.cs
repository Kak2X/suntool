using System.Diagnostics;
using SunCommon;
namespace SunDis;

public class PointerTable : RomData, IFileSplit
{
    public readonly List<SndHeader> Songs;
    public readonly GbPtr? BankTableLocation;
    private readonly FormatOptions _opt;
    private readonly Stream _s;

    public PointerTable(Stream s, int songCount, GbPtr? bankTablePtr, FormatOptions opt) : base(s)
    {
        _s = s;
        _opt = opt;

        BankTableLocation = bankTablePtr;
        Songs = new List<SndHeader>(songCount);
        var songPtrs = ReadSongPtrs(s, songCount);
        var i = 1;
        foreach (var songPtr in songPtrs.Skip(1))
        {
            var existingSong = Songs.FirstOrDefault(x => x.Location == songPtr);
            if (existingSong != null)
                Songs.Add(existingSong);
            else
            {
                s.Seek(songPtr.RomAddress, SeekOrigin.Begin);
                try
                {
                    Songs.Add(new SndHeader(s, new SongInfo(i), opt));
                }
                catch (InvalidSongHeaderException e) 
                {
                    Console.WriteLine(e);
                }
            }
            i++;
        }
    }

    public OpcodeDump GetRomDump(GapMode mode)
    {
        var romData = BuildOpcodeMap();
        FillGaps(romData, mode);
        MergeMacroLength(romData);
        return new OpcodeDump(romData);
    }

    private SortedSet<RomData> BuildOpcodeMap()
    {
        var romData = new SortedSet<RomData>();
        romData.Add(this);

        // Build set
        foreach (var songHdr in Songs)
            if (romData.Add(songHdr))
                foreach (var songChHdr in songHdr.Channels)
                    AddToOpcodeMap(romData, songChHdr);

        return romData;
    }

    private void AddToOpcodeMap(ISet<RomData> romData, SndChHeader songChHdr)
    {
        if (romData.Add(songChHdr))
        {
            AddOpcodes(songChHdr.Data.Main);
            foreach (var fnc in songChHdr.Data.Subs)
                AddOpcodes(fnc);
        }
        void AddOpcodes(SndFunc func)
        {
            foreach (var x in func.Opcodes)
                romData.Add(x);
        }
    }

    private struct SndChThreshold
    {
        public Dictionary<SndChPtrNum, long> Ch = [];
    
        public SndChThreshold()
        {
            Clear();
        }
    
        public void Clear()
        {
            Ch[SndChPtrNum.SND_CH1_PTR] = long.MaxValue;
            Ch[SndChPtrNum.SND_CH2_PTR] = long.MaxValue;
            Ch[SndChPtrNum.SND_CH3_PTR] = long.MaxValue;
            Ch[SndChPtrNum.SND_CH4_PTR] = long.MaxValue;
        }
    
        public SndChPtrNum FindFirst(long pos)
        {
            var res = SndChPtrNum.SND_CH1_PTR;
            foreach (var x in Ch)
                if (pos >= x.Value && res < x.Key) // pos >= threshold
                    res = x.Key;
            return res;
        }
    }
    
    private void FillGaps(SortedSet<RomData> romData, GapMode mode)
    {
        // Find gaps (either unreferenced or padded)
        RomData? last = null;
        long? expectedNext = null;
        var sndChMap = new SndChThreshold();

        var fakeState = new SongInfo(0);
        var gapData = new HashSet<RomData>();
        foreach (var x in romData)
        {
            //--
            // Update state as needed
            if (x is SndHeader xHead)
            {
                fakeState.Id = xHead.Id;
                sndChMap.Clear();
            }
            else if (x is SndChHeader xChHead)
            {
                fakeState.ChPtr = xChHead.SoundChannelPtr;
                fakeState.IsSfx = xChHead.InitialStatus.HasFlag(SndInfoStatus.SIS_SFX);
                sndChMap.Ch[xChHead.SoundChannelPtr] = xChHead.Data.Location.RomAddress;
            }
            //--

            if (expectedNext.HasValue && expectedNext != x.Location.RomAddress)
            {
                Debug.Assert(last is not null);
                Debug.Assert(expectedNext < x.Location.RomAddress, "Inconsistent block size.");

                var expectedNextPtr = GbPtrPool.Create(expectedNext.Value); // For display only
                _s.Seek(expectedNext.Value, SeekOrigin.Begin);

                // Try to autodetect what this unreferenced block contains
                if (last is PointerTable || mode == GapMode.ByteOnly)
                {
                    Console.WriteLine($"Filling gap between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");
                    gapData.Add(new GenericByteArray(_s, x.Location.RomAddress));
                }
                else if (last.Location.Bank != x.Location.Bank) // checking lastLoc just in case the old bank is filled
                {
                    Console.WriteLine($"Switched banks ({last.Location.Bank:X2} -> {x.Location.Bank:X2}) from {expectedNextPtr.ToBankString()} to {x.Location.ToBankString()}");
                    // Split the labeled padding between the two banks
                    var splitPoint = GbPtrPool.Create(x.Location.Bank, 0x4000).RomAddress;
                    if (expectedNext.Value < splitPoint)
                        gapData.Add(new GenericByteArray(_s, splitPoint));
                    gapData.Add(new GenericByteArray(_s, x.Location.RomAddress));
                }
                else if (last is SndChHeader lastChHeader)
                {
                    Console.WriteLine($"Unreferenced sound channel between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");
                    // Assume that any unreferenced data right after a sound channel header is also a sound channel header
                    var header = new SndChHeader(_s, fakeState, _opt);
                    AddToOpcodeMap(gapData, header);
                    sndChMap.Ch[fakeState.ChPtr] = header.Data.Location.RomAddress;
                }
                else
                {
                    Console.WriteLine($"Gap detected between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");

                    var parser = OpcodeParser.Create(_opt);
                    var first = true;
                    fakeState.ChPtr = sndChMap.FindFirst(_s.Position);
                    while (_s.Position < x.Location.RomAddress)
                    {
                        var opcode = parser.Parse(_s, fakeState);
                        if (first)
                        {
                            opcode.Location.Label = $"SndData_Unused_{opcode.Location.RomAddress:X8}";
                            first = false;
                        }
                        gapData.Add(opcode);
                    }
                }
            }
            expectedNext = x.Location.RomAddress + x.SizeInRom();
            last = x;
        }

        foreach (var x in gapData)
            romData.Add(x);
    }

    private static void MergeMacroLength(SortedSet<RomData> opcodes)
    {
        var toDelete = new List<RomData>();
        RomData? last = null;
        foreach (var x in opcodes)
        {
            if (last is IMacroLength lastCmd && x is CmdWait cmdLen)
            {
                lastCmd.Length = cmdLen.Length;
                toDelete.Add(x);
            }
            last = x;
        }
        foreach (var x in toDelete)
            opcodes.Remove(x);
    }

    private GbPtr[] ReadSongPtrs(Stream s, int songCount)
    {
        var ptrs = s.ReadUint16s(songCount);
        if (BankTableLocation is null)
        {
            return ptrs.Select(x => GbPtrPool.Create(Location.Bank, x)).ToArray();
        }
        else
        {
            s.Seek(BankTableLocation.RomAddress, SeekOrigin.Begin);
            var banks = s.ReadUint8s(songCount);

            return Enumerable.Range(0, songCount).Select(i => GbPtrPool.Create(banks[i], ptrs[i])).ToArray();
        }
    }

    public override int SizeInRom()
    {
        return Songs.Count * (BankTableLocation is not null ? 3 : 2);
    }

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteLine("Sound_SndHeaderPtrTable:");
        for (var i = 0; i < Songs.Count; i++)
            sw.WriteIndent($"dw {Songs[i].Location.ToLabel()} ; ${(i + 0x80):X2}");

        if (BankTableLocation is not null)
        {
            sw.WriteLine("Sound_SndBankPtrTable:");
            for (var i = 0; i < Songs.Count; i++)
                sw.WriteIndent($"db BANK({Songs[i].Location.ToLabel()}) ; ${(i + 0x80):X2}");
        }
    }

    public string GetFilename()
    {
        return "sound_headers.asm";
    }
}
