namespace SunCommon;

public class CmdErr : SndOpcode
{
    public int Cmd { get; set; }
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("snderr", Cmd.AsHexByte());
    }
}
