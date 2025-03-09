namespace SunCommon;

public class CmdVibratoOp : SndOpcode
{
    public int VibratoId { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("vibrato_on", VibratoId.AsHexByte());
    }
}
