namespace SunDis;

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
