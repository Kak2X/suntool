namespace SunCommon;

public class CmdFineTune : SndOpcode
{
    public int Offset { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("fine_tune", $"{Offset.ToSigned()}");
    }
}
