namespace SunDis;

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
