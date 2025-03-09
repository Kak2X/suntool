namespace SunCommon;

public class CmdClrVibrato : SndOpcode
{
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("vibrato_off");
    }
}
