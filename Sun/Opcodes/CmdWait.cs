namespace SunnyDay;

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
