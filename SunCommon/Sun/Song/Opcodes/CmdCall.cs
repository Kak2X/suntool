namespace SunCommon;

public class CmdCall : SndOpcode, IHasPointer
{
    public SndOpcode? Target { get; set; }
    public override int SizeInRom() => 3;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        Target.EnsureSet();
        sw.WriteCommand("snd_call", Target.GetLabel());
    }
}
