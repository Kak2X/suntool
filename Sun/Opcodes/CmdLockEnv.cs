namespace SunnyDay;

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
