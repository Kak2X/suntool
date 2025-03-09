namespace SunCommon;

public class CmdLoopCnt : SndOpcode, IHasPointer
{
    public int TimerId { get; set; }
    public int TimerVal { get; set; }
    public SndOpcode? Target { get; set; }
    public override int SizeInRom() => 5;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        Target.EnsureSet();
        sw.WriteCommand("snd_loop", Target.GetLabel(), TimerId.AsHexByte(), $"{TimerVal}");
    }
}
