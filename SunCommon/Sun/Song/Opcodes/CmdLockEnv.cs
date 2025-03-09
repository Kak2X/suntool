namespace SunCommon;

public class CmdLockEnv : SndOpcode
{
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("lock_envelope");
    }
}
