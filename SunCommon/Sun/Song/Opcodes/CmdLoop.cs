namespace SunCommon;

public class CmdLoop : SndOpcode, IHasPointer
{
    public CmdLoop()
    {
        Terminates = true;
    }
    public SndOpcode? Target { get; set; }
    public override int SizeInRom() => 3;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        Target.EnsureSet();
        sw.WriteCommand("snd_loop", Target.GetLabel());
    }
}
