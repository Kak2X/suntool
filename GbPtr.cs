using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Emit;
using static System.Net.Mime.MediaTypeNames;
namespace SunnyDay;

public class GbPtr : IEquatable<GbPtr>, IComparable, IComparable<GbPtr>
{
    public readonly int Bank;
    public readonly int Address;
    public readonly long RomAddress;
    public string? Label = null;

    private const int BankSize = 0x4000;

    public GbPtr(int bank, int address)
    {
        Bank = bank;
        Address = address;
        RomAddress = (bank * BankSize) + (address >= BankSize ? address - BankSize : address);
        //Label = $"L{Bank:X2}{Address:X4}";
    }

    public GbPtr(long romAddress)
    {
        Bank = (int)(romAddress / BankSize);
        Address = (int)(romAddress % BankSize) + (Bank > 0 ? BankSize : 0);
        RomAddress = romAddress;
        //Label = $"L{Bank:X2}{Address:X4}";
    }
    public void SetLabel(string v) => Label = v;
    public bool SetLabelIfNew(string v)
    {
        if (Label != null)
            return false;
        Label = v;
        return true;
    }

    public string ToBankString()
    {
        return $"{Bank:X2}:{Address:X4}";
    }

    public string ToDefaultLabel()
    {
        return $"L{Bank:X2}{Address:X4}";
    }

    public string ToLabel()
    {
        return Label ?? ToDefaultLabel();
    }

    public static GbPtr Create(string loc)
    {
        var x = loc.Split(':');
        return x.Length switch
        {
            2 => new GbPtr(int.Parse(x[0], NumberStyles.HexNumber), int.Parse(x[1], NumberStyles.HexNumber)),
            1 => new GbPtr(int.Parse(x[0], NumberStyles.HexNumber)),
            _ => throw new ArgumentException($"Pointer '{loc}' isn't valid.", nameof(loc))
        };
    }

    public static int GetBank(long romAddr) => (int)(romAddr / BankSize);



    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        if (obj is null)
            return false;
        return obj is GbPtr ptr && Equals(ptr);
    }

    public override int GetHashCode() => HashCode.Combine(RomAddress);
    public bool Equals(GbPtr? other) => other is not null && RomAddress == other.RomAddress;
    public int CompareTo(GbPtr? other) => RomAddress.CompareTo(other?.RomAddress ?? -1);
    public int CompareTo(object? obj) => RomAddress.CompareTo((obj as GbPtr)?.RomAddress ?? -1);
    public static bool operator ==(GbPtr left, GbPtr right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(GbPtr left, GbPtr right) => !(left == right);
    public static bool operator <(GbPtr left, GbPtr right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(GbPtr left, GbPtr right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(GbPtr left, GbPtr right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator >=(GbPtr left, GbPtr right) => left is null ? right is null : left.CompareTo(right) >= 0;


    //public override bool Equals(object? obj) => obj is GbPtr ptr && Equals(ptr);
    //public bool Equals(GbPtr other) => RomAddress == other.RomAddress;
    //public override int GetHashCode() => HashCode.Combine(RomAddress);
    //public int CompareTo(object? obj) => CompareTo((GbPtr)obj);
    //public int CompareTo(GbPtr other) => RomAddress.CompareTo(other.RomAddress);
    //public static bool operator ==(GbPtr left, GbPtr right) => left.Equals(right);
    //public static bool operator !=(GbPtr left, GbPtr right) => !(left == right);
    //public static bool operator <(GbPtr left, GbPtr right) => left.CompareTo(right) < 0;
    //public static bool operator <=(GbPtr left, GbPtr right) => left.CompareTo(right) <= 0;
    //public static bool operator >(GbPtr left, GbPtr right) => left.CompareTo(right) > 0;
    //public static bool operator >=(GbPtr left, GbPtr right) => left.CompareTo(right) >= 0;
}

// TODO: pool shouldn't be static...
public static class GbPtrPool
{

    private static HashSet<GbPtr> _cache = [];

    public static void Clear()
    {
        _cache.Clear();
    }

    private static GbPtr GetCached(GbPtr ptr)
    {
        if (_cache.TryGetValue(ptr, out var found))
        {
            return found;
        }
        else
        {
            _cache.Add(ptr);
            return ptr;
        }
    }

    public static GbPtr Create(int bank, int address) => GetCached(new GbPtr(bank, address));
    public static GbPtr Create(long romAddress) => GetCached(new GbPtr(romAddress));
    public static GbPtr Create(string loc) => GetCached(GbPtr.Create(loc));
}

public abstract class RomData : IEquatable<RomData?>, IComparable
{
    public readonly GbPtr Location;
    
    /// <summary>
    ///     Size of the structure, WITHOUT any pointed entries.
    /// </summary>
    public abstract int SizeInRom();

    //public abstract string ToDisasm();


    public RomData(Stream s)
    {
        Location = GbPtrPool.Create(s.Position);
    }
    public RomData(GbPtr location)
    {
        Location = location;
    }

    public override bool Equals(object? obj) => Equals(obj as RomData);
    public bool Equals(RomData? other) => other is not null && Location.Equals(other.Location);
    public override int GetHashCode() => HashCode.Combine(Location);
    public int CompareTo(object? obj) => ((IComparable)Location).CompareTo(((RomData)obj).Location);
    public static bool operator ==(RomData? left, RomData? right) => EqualityComparer<RomData>.Default.Equals(left, right);
    public static bool operator !=(RomData? left, RomData? right) => !(left == right);
    public abstract void WriteToDisasm(MultiWriter sw);
}

public class FormatOptions
{
    public DataMode Mode;
    public enum DataMode
    {
        KOF96,
        OP,
    }
}
public enum GapMode
{
    Ignore,
    ByteOnly,
    TryDecode,
}

public class OpcodeDump(SortedSet<RomData> opcodes)
{
    public void ToDisassembly(string path)
    {

        // optimize length, not ideal done here but whatever

        var importantPtrs = new HashSet<GbPtr>();
        foreach (var x in opcodes.OfType<IHasPointer>()) // doing this for sound data only
        {
            importantPtrs.Add(x.Target);
        }

        using (var w = new MultiWriter(path))
        {
            // Split when crossing over lines
            Debug.Assert(opcodes.First() is IFileSplit, "This shouldn't happen, exception incoming.");
            foreach (var opcode in opcodes)
            {
                // Split point
                if (opcode is IFileSplit splitable)
                    w.ChangeFile(splitable.GetFilename());
                if (opcode.Location.Label != null || importantPtrs.Contains(opcode.Location))
                    w.WriteLine($"{opcode.Location.ToLabel()}:");
                opcode.WriteToDisasm(w);
            }

            w.ChangeFile("main.asm", false);
            foreach (var x in w.FileHistory)
                w.WriteLine($"INCLUDE \"{x.Replace('\\', '/')}\"");
        }
    }

    
}
public class MultiWriter(string path) : IDisposable
{
    private FileStream _fs = null!;
    private StreamWriter _sw = null!;

    public List<string> FileHistory = [];

    public void ChangeFile(string file, bool log = true)
    {
        if (_sw != null)
        {
            _sw.Flush();
            _sw.Dispose();
            _fs!.Dispose();
        }

        // Create folders as needed
        var fullPath = Path.Combine(path, file);
        var targetDir = Path.GetDirectoryName(fullPath);
        if (targetDir != null && !File.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        _fs = new FileStream(fullPath, FileMode.Create);
        _sw = new StreamWriter(_fs);
        if (log)
            FileHistory.Add(file);
    }

    public void WriteLine(string line) => _sw.WriteLine(line);
    public void WriteIndent(string line) => _sw.WriteLine($"\t{line}");
    public void WriteCommand(string command, params string[] args)
    {
        var strArgs = args.Length > 0 ? $" {string.Join(", ", args)}" : string.Empty;
        _sw.WriteLine($"\t{command}{strArgs}"); // .PadRight(10)
    }

    private bool _disposed;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sw?.Flush();
                _sw?.Dispose();
                _fs?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class SongInfo
{
    public SongInfo(int id)
    {
        Id = id;
    }

    public int Id;
    public bool IsSfx;
    public int ChNum;
    public int LoopCount;

    public string TypeString => IsSfx ? "SFX" : "BGM";
    private SndChPtrNum _chPtr;
    public SndChPtrNum ChPtr
    {
        get => _chPtr;
        set
        {
            _chPtr = value;
            ChNum = value.Normalize();
        }
    }
}

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
                Songs.Add(new SndHeader(s, new SongInfo(i), opt));
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

    // private void BuildSndChMap(SortedSet<RomData> romData)
    // {
    //     foreach (var x in)
    // }

    // private struct SndChThreshold
    // {
    //     public Dictionary<SndChPtrNum, long> Ch = [];
    //
    //     public SndChThreshold()
    //     {
    //         Clear();
    //     }
    //
    //     public void Clear()
    //     {
    //         Ch[SndChPtrNum.SND_CH1_PTR] = long.MaxValue;
    //         Ch[SndChPtrNum.SND_CH2_PTR] = long.MaxValue;
    //         Ch[SndChPtrNum.SND_CH3_PTR] = long.MaxValue;
    //         Ch[SndChPtrNum.SND_CH4_PTR] = long.MaxValue;
    //     }
    //
    //     public SndChPtrNum FindFirst(long pos)
    //     {
    //         var res = SndChPtrNum.SND_CH1_PTR;
    //         foreach (var x in Ch)
    //             if (pos >= x.Value && res < x.Key) // pos >= threshold
    //                 res = x.Key;
    //         return res;
    //     }
    // }
    //
    private void FillGaps(SortedSet<RomData> romData, GapMode mode)
    {
        // Find gaps (either unreferenced or padded)
        RomData? last = null;
        long? expectedNext = null;
      //  var sndChMap = new SndChThreshold();

        var fakeState = new SongInfo(0);
        var gapData = new HashSet<RomData>();
        foreach (var x in romData)
        {
            //--
            // Update state as needed
            if (x is SndHeader xHead)
                fakeState.Id = xHead.Id;
            else if (x is SndChHeader xChHead)
            {
                fakeState.ChPtr = xChHead.SoundChannelPtr;
                fakeState.IsSfx = xChHead.InitialStatus.HasFlag(SndInfoStatus.SIS_SFX);
            }
            //--

         //  if (mode != GapMode.ByteOnly && x is SndHeader sndHeader)
         //  {
         //      sndChMap.Clear();
         //      foreach (var y in sndHeader.Channels)
         //          sndChMap.Ch[y.SoundChannelPtr] = y.Data.Location.RomAddress;
         //  }

            if (expectedNext.HasValue && expectedNext != x.Location.RomAddress)
            {
                Debug.Assert(last is not null);
                Debug.Assert(expectedNext < x.Location.RomAddress, "Inconsistent block size.");

                var expectedNextPtr = GbPtrPool.Create(expectedNext.Value); // For display only
                _s.Seek(expectedNext.Value, SeekOrigin.Begin);

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
                }
                else
                {
                    Console.WriteLine($"Gap detected between {expectedNextPtr.ToBankString()} and {x.Location.ToBankString()}");

                    var parser = OpcodeParser.Create(_opt);
                   // var sndChPtr = sndChMap.FindFirst(_s.Position);
                    var first = true;
                   // var fakeState = new SongInfo(9999) { ChPtr = sndChPtr }; // TODO: store copies at the start of the song?
                    while (_s.Position < x.Location.RomAddress)
                    {
                        var opcode = parser.Parse(_s, fakeState);
                        if (first)
                        {
                            opcode.Location.SetLabel($"SndData_Unused_{opcode.Location.RomAddress:X8}");
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
        var ptrs = s.ReadWords(songCount);
        if (BankTableLocation is null)
        {
            return ptrs.Select(x => GbPtrPool.Create(Location.Bank, x)).ToArray();
        }
        else
        {
            s.Seek(BankTableLocation.RomAddress, SeekOrigin.Begin);
            var banks = s.ReadBytes(songCount);

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

public class SndHeader : RomData, IFileSplit
{
    public readonly List<SndChHeader> Channels;
    public readonly int Id;
    public readonly bool IsSfx;

    public SndHeader(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        var count = s.ReadByte();
        Debug.Assert(count > 0 && count <= 4, "Channel count out of range.");

        Channels = new List<SndChHeader>(count);
        for (var i = 0; i < count; i++)
            Channels.Add(new SndChHeader(s, song, opt));

        // IsSfx / TypeString is defined by here
        Id = song.Id;
        IsSfx = song.IsSfx;
        Location.SetLabel($"SndHeader_{song.TypeString}_{song.Id:X2}");
    }

    public string GetFilename()
    {
        var s = IsSfx ? "sfx" : "bgm";
        return $"{s}\\{s}_{Id:x2}.asm";
    }

    public override int SizeInRom()
    {
        return 1;
    }

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteIndent($"db ${Channels.Count:X2} ; Number of channels");
    }
}

public class SndChHeader : RomData
{
    public SndInfoStatus InitialStatus;
    public SndChPtrNum SoundChannelPtr;
    public SndData Data;
    public SciNote? BaseNote;
    public byte Unused;

    public SndChHeader(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        InitialStatus = (SndInfoStatus)s.ReadByte();
        SoundChannelPtr = (SndChPtrNum)s.ReadByte();
        var sndDataPointer = s.ReadLocalPtr();
        BaseNote = SciNote.Create(s.ReadByte());
        Unused = (byte)s.ReadByte();

        song.IsSfx = InitialStatus.HasFlag(SndInfoStatus.SIS_SFX);
        song.ChPtr = SoundChannelPtr;

        var originalPos = s.Position;
        s.Seek(sndDataPointer.RomAddress, SeekOrigin.Begin);
        Data = new SndData(s, song, opt);
        s.Seek(originalPos, SeekOrigin.Begin);

        Location.SetLabel($".ch{song.ChNum}");
    }

    public override int SizeInRom() => 6;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteIndent($"db {InitialStatus.GenerateConstLabel()} ; Initial playback status");
        sw.WriteIndent($"db {SoundChannelPtr} ; Sound channel ptr");
        sw.WriteIndent($"dw {Data.Location.ToLabel()} ; Data ptr");
        sw.WriteIndent($"{SciNote.ToDnote(BaseNote)} ; Base note");
        sw.WriteIndent($"db ${Unused:X2} ; Unused");
    }
}

public class SndData : RomData
{
    public readonly SndFunc Main;
    public readonly List<SndFunc> Subs = [];

    public SndData(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        Location.SetLabel($"SndData_{song.TypeString}_{song.Id:X2}_Ch{song.ChNum}");

        Main = new SndFunc(s, song, opt);
        // Find calls from main function
        var uniqueCalls = new HashSet<GbPtr>();
        foreach (var x in Main.Opcodes.OfType<CmdCall>())
            uniqueCalls.Add(x.Target);

        // Traverse nested subroutines.
        // This uses a second buffer to store newly found unique nested calls,
        // which is swapped at the end with the processed ones.
        var toProc = uniqueCalls.ToList();
        var toProcNext = new List<GbPtr>();
        do
        {
            toProcNext.Clear();
            foreach (var call in toProc)
            {
                //--
                s.Seek(call.RomAddress, SeekOrigin.Begin);
                var sub = new SndFunc(s, song, opt);
                foreach (var x in sub.Opcodes.OfType<CmdCall>())
                    if (uniqueCalls.Add(x.Target))
                        toProcNext.Add(x.Target); // "recursion"
                Subs.Add(sub);
                //--
            }
            (toProc, toProcNext) = (toProcNext, toProc);
        } while (toProc.Count > 0); // after swap

        // Label the subroutines sequentially, by ROM order
        var subNo = 0;
        foreach (var x in Subs.OrderBy(x => x.Location))
        {
            x.Location.SetLabel($"SndCall_{song.TypeString}_{song.Id:X2}_Ch{song.ChNum}_{subNo:X}");
            subNo++;
        }
    }

    public override int SizeInRom() => 0;

    public override void WriteToDisasm(MultiWriter sw)
    {
    }
}

public class SndFunc : RomData
{
    public readonly List<SndOpcode> Opcodes = [];

    public SndFunc(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        var parser = OpcodeParser.Create(opt);
        song.LoopCount = 0;

        SndOpcode cmd;
        do
        {
            cmd = parser.Parse(s, song);
            Opcodes.Add(cmd);
        } while (!cmd.Terminates);

        // Self-check that all non-call pointers point inside the function.
        long min = Location.RomAddress, max = s.Position;
        foreach (var x in Opcodes.OfType<IHasPointer>())
            if (x is not CmdCall && (x.Target.RomAddress < min || x.Target.Address >= max))
                throw new Exception($"Unsupported jump pointer outside function at 0x{x.Target.RomAddress:X08}");
    }

    public override int SizeInRom() => 0;

    public override void WriteToDisasm(MultiWriter sw)
    {
    }
}


/// <summary>For commands whose macros support a length parameter.</summary>
public interface IMacroLength
{
    int? Length { get; set; }
}

public interface IHasPointer
{
    GbPtr Target { get; set; }
}

public interface IFileSplit
{
    string GetFilename();
}

public class GenericByteArray : RomData, IFileSplit
{
    public readonly byte[] Data;

    public GenericByteArray(Stream s, long targetPos) : base(s)
    {
        Location.SetLabel($"Padding_{Location.RomAddress:X8}");
        var length = (int)(targetPos - s.Position);
        Data = new byte[length];
        s.Read(Data, 0, length);
    }

    public string GetFilename() => $"padding/{Location.ToDefaultLabel()}.asm";

    public override int SizeInRom() => Data.Length;

    public override void WriteToDisasm(MultiWriter sw)
    {
        foreach (var x in Data)
            sw.WriteIndent($"db ${x:X2}");
    }
}

public abstract class SndOpcode : RomData
{
    public bool Terminates { get; init; }

    public SndOpcode(GbPtr p) : base(p)
    {
    }
}

public class CmdErr : SndOpcode
{
    public readonly int Cmd;

    public CmdErr(GbPtr p, int cmd) : base(p)
    {
        Cmd = cmd;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snderr", $"${Cmd:X2}");
    }
}

public class CmdChanStop : SndOpcode
{
    public readonly PriorityGroup? Priority;
    public CmdChanStop(GbPtr p, PriorityGroup? priority = null) : base(p)
    {
        Terminates = true;
        Priority = priority;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        if (Priority.HasValue)
            sw.WriteCommand("chan_stop", Priority.Value.ToString());
        else
            sw.WriteCommand("chan_stop");
    }
}

public class CmdEnvelope : SndOpcode
{
    public readonly int Arg;

    public CmdEnvelope(GbPtr p, Stream s) : base(p)
    {
        Arg = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("envelope", $"${Arg:X2}");
    }
}

public class CmdWaveVol : SndOpcode
{
    public readonly int Vol;

    public CmdWaveVol(GbPtr p, Stream s) : base(p)
    {
        // Normalized, making it comparable to CndEnv
        Vol = s.ReadByte() << 1;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wave_vol", $"${Vol:X2}");
    }
}

/*
public class CmdEnv : SndOpcode
{
    public readonly int Vol;
    public readonly bool SweepUp;
    public readonly int Sweep;

    public CmdEnv(GbPtr p, Stream s) : base(p)
    {
        var x = s.ReadByte();
        // for ch1-2-4
		Sweep = x & 0b111;
		SweepUp = ((x >> 3) & 0b1) != 0; 
		Vol = (x >> 4) & 0b1111;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteLine($"\tsndenv {Vol}, {(SweepUp ? "SNDENV_INC" : "SNDENV_DEC")}, {Sweep}");
    }
}

public class CmdEnvCh3 : SndOpcode
{
    public readonly int Vol;

    public CmdEnvCh3(GbPtr p, Stream s) : base(p)
    {
        // Normalized, making it comparable to CndEnv
        Vol = (s.ReadByte() & 0b01100000) >> 3;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteLine($"\tsndenvch3 {Vol}");
    }
}
*/

public class CmdLoop : SndOpcode, IHasPointer
{
    public GbPtr Target { get; set; }

    public CmdLoop(GbPtr p, Stream s, SongInfo song) : base(p)
    {
        Terminates = true;
        Target = s.ReadLocalPtr();
        if (Target.SetLabelIfNew($".loop{song.LoopCount}"))
            song.LoopCount++;
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_loop", Target.ToLabel());
    }
}

public class CmdFineTune : SndOpcode
{
    public readonly int Offset;

    public CmdFineTune(GbPtr p, Stream s) : base(p)
    {
        Offset = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("fine_tune", $"{Offset}");
    }
}

public class CmdLoopCnt : SndOpcode, IHasPointer
{
    public readonly int TimerId;
    public readonly int TimerVal;
    public GbPtr Target { get; set; }

    public CmdLoopCnt(GbPtr p, Stream s, SongInfo song) : base(p)
    {
        TimerId = s.ReadByte();
        TimerVal = s.ReadByte();
        Target = s.ReadLocalPtr();
        if (Target.SetLabelIfNew($".loop{song.LoopCount}"))
            song.LoopCount++;
    }

    public override int SizeInRom() => 5;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_loop", Target.ToLabel(), $"${TimerId:X2}", $"{TimerVal}");
    }
}

public class CmdSweep : SndOpcode
{
    public readonly int Arg;

    public CmdSweep(GbPtr p, Stream s) : base(p)
    {
        Arg = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("sweep", $"${Arg:X2}");
    }
}
/*
public class CmdCh1Sweep : SndOpcode
{
    public readonly int Time;
    public readonly bool Inc;
    public readonly int Num;

    public CmdCh1Sweep(GbPtr p, Stream s) : base(p)
    {
        var x = s.ReadByte();
        Time = (x >> 4) & 0b111;
        Inc = ((x >> 3) & 0b1) != 0;
        Num = x & 0b111;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteLine($"\tsnd_UNUSED_nr10 {Time}, {(Inc ? "SNDPRD_INC" : "SNDPRD_DEC")}, {Num}");
    }
}
 */

public class CmdEnaCh : SndOpcode
{
    public readonly int Pan; // Nr51

    public CmdEnaCh(GbPtr p, Stream s) : base(p)
    {
        Pan = s.ReadByte(); // (Nr51)
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("panning", $"${Pan:X2}"); // Pan.GenerateConstLabel());
    }
}

public class CmdCall : SndOpcode, IHasPointer
{
    public GbPtr Target {  get; set; }

    public CmdCall(GbPtr p, Stream s) : base(p)
    {
        Target = s.ReadLocalPtr();
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_call", Target.ToLabel());
    }
}

public class CmdRet : SndOpcode
{
    public CmdRet(GbPtr p) : base(p)
    {
        Terminates = true;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_ret");
    }
}

public class CmdDutyCycle : SndOpcode
{
    public readonly int Duty;
    public readonly int Length;

    public CmdDutyCycle(GbPtr p, Stream s) : base(p)
    {
        var x = s.ReadByte();
        Duty = (x >> 6) & 0b11;
        Length = x & 0b111111;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var args = new List<string>(2) { $"{Duty}" };
        if (Length > 0)
            args.Add($"{Length}");
        sw.WriteCommand("duty_cycle", [.. args]);
    }
}
/*
public class CmdCutoff : SndOpcode
{
    public readonly int Length;

    public CmdCutoff(GbPtr p, Stream s) : base(p)
    {
        Length = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("cutoff", $"{Length}");
    }
}


public class CmdLenCh12 : SndOpcode
{
    public readonly int Duty;
    public readonly int Length;
    public readonly SndChPtrNum Ch;

    public CmdLenCh12(GbPtr p, Stream s, SndChPtrNum ch) : base(p)
    {
        var x = s.ReadByte();
        Duty = (x >> 6) & 0b11;
        Length = x & 0b111111;
        Ch = ch;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var cmd = Ch == SndChPtrNum.SND_CH1_PTR ? "sndnr11" : "sndnr21";
        sw.WriteLine($"\t{cmd} {Duty}, {Length}");
    }
}
public class CmdLenCh34 : SndOpcode
{
    public readonly int Length;
    public readonly SndChPtrNum Ch;

    public CmdLenCh34(GbPtr p, Stream s, SndChPtrNum ch) : base(p)
    {
        Length = s.ReadByte();
        Ch = ch;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var cmd = Ch == SndChPtrNum.SND_CH3_PTR ? "sndnr31" : "sndnr41";
        sw.WriteLine($"\t{cmd} {Length}");
    }
}
*/
public class CmdLockEnv : SndOpcode
{
    public CmdLockEnv(GbPtr p) : base(p)
    {
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("lock_envelope");
    }
}
public class CmdUnlockEnv : SndOpcode
{
    public CmdUnlockEnv(GbPtr p) : base(p)
    {
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("unlock_envelope");
    }
}

public class CmdVibrato96 : SndOpcode
{
    public CmdVibrato96(GbPtr p) : base(p)
    {
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("vibrato");
    }
}

public class CmdVibratoOp : SndOpcode
{
    public readonly int VibratoId;

    public CmdVibratoOp(GbPtr p, Stream s) : base(p)
    {
        VibratoId = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("vibrato_on", $"${VibratoId:X2}");
    }
}

public class CmdClrVibrato : SndOpcode
{
    public CmdClrVibrato(GbPtr p) : base(p)
    {
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("vibrato_off");
    }
}

public class CmdWave : SndOpcode
{
    public readonly int WaveId;

    public CmdWave(GbPtr p, Stream s) : base(p)
    {
        WaveId = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wave_id", $"${WaveId:X2}");
    }
}

public class CmdWaveCutoff : SndOpcode
{
    public readonly int Length;

    public CmdWaveCutoff(GbPtr p, Stream s) : base(p)
    {
        Length = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wave_cutoff", $"{Length}");
    }
}

public class CmdWaitLong : SndOpcode
{
    public readonly int Length;

    public CmdWaitLong(GbPtr p, Stream s) : base(p)
    {
        Length = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wait2", $"{Length}");
    }
}

public class CmdSlide : SndOpcode
{
    public readonly int Length;
    public readonly SciNote? Note;

    public CmdSlide(GbPtr p, Stream s) : base(p)
    {
        Length = s.ReadByte();
        Note = SciNote.Create(s.ReadByte());
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("pitch_slide", SciNote.ToNoteAsm(Note), $"{Length}");
    }
}

public class CmdNoisePoly : SndOpcode, IMacroLength
{
    public readonly SciNote? Note;
    public int? Length { get; set; }

    public CmdNoisePoly(GbPtr p, int cmd) : base(p)
    {
        //Console.Clear();
        //var set = new[]{
        //    0xD7,0xD6,0xD5,0xD4,0xC7,0xC6,0xC5,0xC4,0xB7,0xB6,0xB5,0xB4,
        //    0xA7,0xA6,0xA5,0xA4,0x97,0x96,0x95,0x94,0x87,0x86,0x85,0x84,
        //    0x77,0x76,0x75,0x74,0x67,0x66,0x65,0x64,0x57,0x56,0x55,0x54,
        //    0x47,0x46,0x45,0x44,0x37,0x36,0x35,0x34,0x27,0x26,0x25,0x24,
        //    0x17,0x16,0x15,0x14,0x07,0x06,0x05,0x04,0x03,0x02,0x01,0x00,
        //};
        //foreach (var x in set)
        //{
        //    Console.WriteLine($"{x:X2} -> {SciNote.ToNoteAsm(SciNote.CreateFromNoise(x))}");
        //}
        Note = SciNote.CreateFromNoise(cmd);
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var args = new List<string>();
        if (Note != null)
            args.Add(SciNote.ToNoteAsm(Note));
        if (Length.HasValue)
            args.Add(Length.Value.ToString());

        sw.WriteCommand("note4", [.. args]);
    }
}

public class CmdWait : SndOpcode
{
    public int Length;

    public CmdWait(GbPtr p, int cmd) : base(p)
    {
        Length = cmd;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wait", $"{Length}");
    }
}

public class CmdNote : SndOpcode, IMacroLength
{
    public readonly SciNote? Note;
    public int? Length { get; set; }

    public CmdNote(GbPtr p, int cmd) : base(p)
    {
        Note = SciNote.Create(cmd - (int)SndCmdType.SNDNOTE_BASE);
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var cmd = Note == null ? "silence" : "note";
        var args = new List<string>();
        if (Note != null)
            args.Add(SciNote.ToNoteAsm(Note));
        if (Length.HasValue)
            args.Add(Length.Value.ToString());

        sw.WriteCommand(cmd, [.. args]);
    }
}


public static class OpcodeParser
{
    public static IOpcodeParser Create(FormatOptions opt)
    {
        return opt.Mode == FormatOptions.DataMode.KOF96
            ? new OpcodeParser96()
            : new OpcodeParserOp();
    }
}

public interface IOpcodeParser
{
    public SndOpcode Parse(Stream s, SongInfo song);
}

public class OpcodeParserOp : IOpcodeParser
{
    public SndOpcode Parse(Stream s, SongInfo song)
    {
        var p = GbPtrPool.Create(s.Position);
        var cmd = s.ReadByte();

        if (cmd >= (int)SndCmdType.SNDCMD_BASE)
        {
            cmd -= (int)SndCmdType.SNDCMD_BASE;
            return cmd switch
            {
                0x03 => new CmdChanStop(p),
                0x04 when song.ChPtr == SndChPtrNum.SND_CH3_PTR => new CmdWaveVol(p, s),
                0x04 => new CmdEnvelope(p, s),
                0x05 => new CmdLoop(p, s, song),
                0x06 => new CmdFineTune(p, s),
                0x07 => new CmdLoopCnt(p, s, song),
                0x08 => new CmdSweep(p, s),
                0x09 => new CmdEnaCh(p, s),
                0x0C => new CmdCall(p, s),
                0x0D => new CmdRet(p),
                0x0E when song.ChPtr < SndChPtrNum.SND_CH3_PTR => new CmdDutyCycle(p, s),
                0x0E => throw new Exception("Attempted to use old CmdCutoff"), //new CmdCutoff(p, s),
                0x0F => new CmdLockEnv(p),
                0x10 => new CmdUnlockEnv(p),
                0x11 => new CmdVibratoOp(p, s),
                0x12 => new CmdClrVibrato(p),
                0x13 => new CmdWave(p, s),
                0x14 => new CmdChanStop(p, PriorityGroup.SNP_SFXMULTI),
                0x15 => new CmdWaveCutoff(p, s),
                0x16 => new CmdChanStop(p, PriorityGroup.SNP_SFX4),
                0x1A => new CmdWaitLong(p, s),
                0x1C => new CmdSlide(p, s),
                _ => new CmdErr(p, cmd),
            };
        }
        else if (cmd < (int)SndCmdType.SNDNOTE_BASE)
            return new CmdWait(p, cmd);
        else if (song.ChPtr == SndChPtrNum.SND_CH4_PTR)
            return new CmdNoisePoly(p, cmd);
        else
            return new CmdNote(p, cmd);
    }
}

public class OpcodeParser96 : IOpcodeParser
{
    public SndOpcode Parse(Stream s, SongInfo song)
    {
        var p = GbPtrPool.Create(s.Position);
        var cmd = s.ReadByte();

        if (cmd >= (int)SndCmdType.SNDCMD_BASE)
        {
            cmd -= (int)SndCmdType.SNDCMD_BASE;
            return cmd switch
            {
                0x03 => new CmdChanStop(p),
                0x04 when song.ChPtr == SndChPtrNum.SND_CH3_PTR => new CmdWaveVol(p, s),
                0x04 => new CmdEnvelope(p, s),
                0x05 => new CmdLoop(p, s, song),
                0x06 => new CmdFineTune(p, s),
                0x07 => new CmdLoopCnt(p, s, song),
                0x08 => new CmdSweep(p, s),
                0x09 => new CmdEnaCh(p, s),
                0x0C => new CmdCall(p, s),
                0x0D => new CmdRet(p),
                0x0E when song.ChPtr < SndChPtrNum.SND_CH3_PTR => new CmdDutyCycle(p, s),
                0x0E => throw new Exception("Attempted to use old CmdCutoff"), //new CmdCutoff(p, s),
                0x0F => new CmdLockEnv(p),
                0x10 => new CmdUnlockEnv(p),
                0x11 => new CmdVibrato96(p),
                0x12 => new CmdClrVibrato(p),
                0x13 => new CmdWave(p, s),
                0x14 => new CmdChanStop(p, PriorityGroup.SNP_SFXMULTI),
                0x15 => new CmdWaveCutoff(p, s),
                0x16 => new CmdChanStop(p, PriorityGroup.SNP_SFX4),
                0x1A => new CmdWaitLong(p, s),
                _ => new CmdErr(p, cmd),
            };
        }
        else if (cmd < (int)SndCmdType.SNDNOTE_BASE)
            return new CmdWait(p, cmd);
        else if (song.ChPtr == SndChPtrNum.SND_CH4_PTR)
            return new CmdNoisePoly(p, cmd);
        else
            return new CmdNote(p, cmd);
    }
}

// These are enums so we can do .ToString() on them.
// Their labels match up with what the disassembly uses, do not change them.

public enum SndChPtrNum
{
    SND_CH1_PTR = 0x13,
    SND_CH2_PTR = 0x18,
    SND_CH3_PTR = 0x1D,
    SND_CH4_PTR = 0x22,
}
public static class SndChPtrNumExtensions
{
    public static int Normalize(this SndChPtrNum value)
    {
        return value switch
        {
            SndChPtrNum.SND_CH1_PTR => 1,
            SndChPtrNum.SND_CH2_PTR => 2,
            SndChPtrNum.SND_CH3_PTR => 3,
            SndChPtrNum.SND_CH4_PTR => 4,
            _ => (int)value,
        };
    }

    public static SndChPtrNum Next(this SndChPtrNum value)
    {
        return value switch
        {
            SndChPtrNum.SND_CH1_PTR => SndChPtrNum.SND_CH2_PTR,
            SndChPtrNum.SND_CH2_PTR => SndChPtrNum.SND_CH3_PTR,
            SndChPtrNum.SND_CH3_PTR => SndChPtrNum.SND_CH4_PTR,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "SndChPtrNum.Next on noise."),
        };
    }
}

[Flags]
public enum PriorityGroup
{
    SNP_SFXMULTI = 1 << 7,
    SNP_SFX4 = 1 << 6,
}

public enum SndLen
{
    SNDLEN_INFINITE = 0xFF,
}

[Flags]
public enum SndInfoStatus
{
    SIS_PAUSE = 1 << 0,
    SIS_LOCKNRx2 = 1 << 1,
    SIS_USEDBYSFX = 1 << 2,
    SIS_SFX = 1 << 3,
    SIS_SLIDE = 1 << 5,
    SIS_VIBRATO = 1 << 6,
    SIS_ENABLED = 1 << 7,
}

[Flags]
public enum SndFadeStatus
{
    SFD_FADEIN = 1 << 7,
}

public enum SndCmdType : int
{
    SNDCMD_FADEIN = 0x10,
    SNDCMD_FADEOUT = 0x20,
    SNDCMD_CH1VOL = 0x30,
    SNDCMD_CH2VOL = 0x40,
    SNDCMD_CH3VOL = 0x50,
    SNDCMD_CH4VOL = 0x60,
    SNDCMD_BASE = 0xE0,
    SNDNOTE_BASE = 0x80,
}

public struct SciNote
{
    public readonly NoteDef Note;
    public readonly int Octave;

    public SciNote(NoteDef note, int octave)
    {
        Note = note;
        Octave = octave;
    }

    public static SciNote? Create(int rawValue)
    {
        Debug.Assert(rawValue >= 0 && rawValue <= 255);
        if (rawValue == 0) return null;

        NoteDef note;
        if (rawValue > 0x7F)
        {
            rawValue = rawValue - 0x100;
            note = (NoteDef)(11 + ((rawValue + 1) % 12));
        }
        else
        {
            rawValue--;
            note = (NoteDef)(rawValue % 12);
        }

        var octave = 2 + (int)Math.Floor(rawValue / (decimal)12);
        return new SciNote(note, octave);
    }

    public static SciNote? CreateFromNoise(int rawValue)
    {
        Debug.Assert(rawValue >= 0 && rawValue <= 255);

        var noteTriplet = (rawValue >> 4) + 1;
        var noteMul = (rawValue & 7) < 4 ? 0 : ((noteTriplet % 3) * 4);
        var note = 11 - noteMul - (rawValue & 3);
        var octave = 6 - (noteTriplet / 3);

        return new SciNote((NoteDef)note, octave);
    }

    public static string ToDnote(SciNote? note)
    {
        return $"dnote {ToNoteAsm(note)}";
    }
    public static string ToNoteAsm(SciNote? note)
    {
        return note.HasValue ? $"{note.Value.Note.ToLabel()},{note.Value.Octave}" : "0";
    }
}

public enum NoteDef
{
    C  = 0,
    Ch = 1,
    D = 2,
    Dh = 3,
    E = 4,
    F = 5,
    Fh = 6,
    G = 7,
    Gh = 8,
    A = 9,
    Ah = 10,
    B = 11,
}
public static class NoteDefExtensions
{
    public static string ToLabel(this NoteDef x) => x switch
    {
        NoteDef.C => "C_",
        NoteDef.Ch => "C#",
        NoteDef.D => "D_",
        NoteDef.Dh => "D#",
        NoteDef.E => "E_",
        NoteDef.F => "F_",
        NoteDef.Fh => "F#",
        NoteDef.G => "G_",
        NoteDef.Gh => "G#",
        NoteDef.A => "A_",
        NoteDef.Ah => "A#",
        NoteDef.B => "B_",
        _ => throw new ArgumentOutOfRangeException(nameof(x)),
    };
}

[Flags]
public enum Nr51
{
    SNDOUT_CH1R = 1 << 0,
    SNDOUT_CH2R = 1 << 1,
    SNDOUT_CH3R = 1 << 2,
    SNDOUT_CH4R = 1 << 3,
    SNDOUT_CH1L = 1 << 4,
    SNDOUT_CH2L = 1 << 5,
    SNDOUT_CH3L = 1 << 6,
    SNDOUT_CH4L = 1 << 7,
}

/*
DEF SNDIDREQ_SIZE      EQU $08
DEF SNDINFO_SIZE       EQU $20 ; Size of iSndInfo struct

; wSndFadeStatus

; Notes (sndnote)

 
 */

/*
 SndHeader_BGM_4A:
	db $04 ; Number of channels
.ch1:
	db SIS_ENABLED ; Initial playback status
	db SND_CH1_PTR ; Sound channel ptr
	dw SndData_BGM_4A_Ch1 ; Data ptr
	db $00 ; Base note
	db $81 ; Unused
.ch2:
	db SIS_ENABLED ; Initial playback status
	db SND_CH2_PTR ; Sound channel ptr
	dw SndData_BGM_4A_Ch2 ; Data ptr
	db $00 ; Base note
	db $81 ; Unused
.ch3:
	db SIS_ENABLED ; Initial playback status
	db SND_CH3_PTR ; Sound channel ptr
	dw SndData_BGM_4A_Ch3 ; Data ptr
	db $00 ; Base note
	db $81 ; Unused
.ch4:
	db SIS_ENABLED ; Initial playback status
	db SND_CH4_PTR ; Sound channel ptr
	dw SndData_BGM_4A_Ch4 ; Data ptr
	db $00 ; Base note
	db $81 ; Unused
 */

public static class StreamExtensions
{
    public static int PeekByte(this Stream s)
    {
        var r = s.ReadByte();
        s.Seek(-1, SeekOrigin.Current);
        return r;
    }

    public static byte[] ReadBytes(this Stream s, int count)
    {
        var res = new byte[count];
        s.Read(res, 0, res.Length);
        return res;
    }

    public static int[] ReadWords(this Stream s, int count)
    {
        var res = new int[count];
        for (var i = 0; i < count; i++)
            res[i] = s.ReadWord();
        return res;
    }

    public static int ReadWord(this Stream s)
    {
        var lo = s.ReadByte();
        var hi = s.ReadByte();
        return (hi << 8) + lo;
    }

    public static GbPtr ReadLocalPtr(this Stream s)
    {
        var bank = GbPtr.GetBank(s.Position);
        var ptr = s.ReadWord();
        return GbPtrPool.Create(bank, ptr);
    }
}

public static class NumExtensions
{
    public static string GenerateConstLabel<T>(this T n) where T : Enum
    {
        return n.ToString().Replace(", ", "|");
    }
}