namespace SunCommon;

public class CmdUnlockEnv : SndOpcode
{
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("unlock_envelope");
    }
}
