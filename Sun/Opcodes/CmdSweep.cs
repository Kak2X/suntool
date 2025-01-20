namespace SunnyDay;

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
