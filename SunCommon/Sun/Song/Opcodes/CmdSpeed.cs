namespace SunCommon;

public class CmdSpeed : SndOpcode // 95-only
{
    public int Arg { get; set; }
    public override int SizeInRom() => 3;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("speed", Arg.AsHexWord());
    }
}
