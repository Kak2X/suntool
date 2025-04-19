namespace SunCommon;

public class CmdFineTuneValue : SndOpcode
{
    public int Offset { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("fine_tune_value", $"{Offset.ToSigned()}");
    }
}
