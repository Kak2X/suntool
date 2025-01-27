namespace SunDis;

public class SndChHeader : RomData
{
    public SndInfoStatus InitialStatus;
    public SndChPtrNum SoundChannelPtr;
    public SndData Data;
    public int FineTune;
    public byte Unused;

    public SndChHeader(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        InitialStatus = (SndInfoStatus)s.ReadByte();
        SoundChannelPtr = (SndChPtrNum)s.ReadByte();
        var sndDataPointer = s.ReadLocalPtr();
        FineTune = s.ReadByte();
        Unused = (byte)s.ReadByte();

        song.IsSfx = InitialStatus.HasFlag(SndInfoStatus.SIS_SFX);
        song.ChPtr = SoundChannelPtr;

        var originalPos = s.Position;
        s.Seek(sndDataPointer.RomAddress, SeekOrigin.Begin);
        Data = new SndData(s, song, opt);
        s.Seek(originalPos, SeekOrigin.Begin);

        Location.Label = $".ch{song.ChNum}";
    }

    public override int SizeInRom() => 6;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteIndent($"db {InitialStatus.GenerateConstLabel()} ; Initial playback status");
        sw.WriteIndent($"db {SoundChannelPtr} ; Sound channel ptr");
        sw.WriteIndent($"dw {Data.Location.ToLabel()} ; Data ptr");
        sw.WriteIndent($"db {FineTune.ToSigned()} ; Initial fine tune");
        sw.WriteIndent($"db ${Unused:X2} ; Unused");
    }
}
