namespace SunDis;

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
