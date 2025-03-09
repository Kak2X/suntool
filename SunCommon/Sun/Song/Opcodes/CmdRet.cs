namespace SunCommon;

public class CmdRet : SndOpcode
{
    public CmdRet()
    {
        Terminates = true;
    }
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("snd_ret");
    }
}
