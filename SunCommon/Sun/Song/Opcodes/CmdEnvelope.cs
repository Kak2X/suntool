namespace SunCommon;

public class CmdEnvelope : SndOpcode
{
    public int Arg { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("envelope", Arg.AsHexByte());
    }
}
