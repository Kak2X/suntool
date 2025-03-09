namespace SunCommon;

public class CmdVibrato96 : SndOpcode
{
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("vibrato_on");
    }
}
