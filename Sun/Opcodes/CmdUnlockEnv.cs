namespace SunnyDay;

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
