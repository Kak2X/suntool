using SunCommon;
namespace SunDis;

public class CmdChanStop : SndOpcode
{
    public readonly PriorityGroup? Priority;
    public readonly bool NoFadeChg;

    public CmdChanStop(GbPtr p, PriorityGroup? priority = null, bool noFadeChg = false) : base(p)
    {
        Terminates = true;
        Priority = priority;
        NoFadeChg = noFadeChg;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        if (Priority.HasValue)
            sw.WriteCommand("chan_stop", Priority.Value.ToString());
        else if (NoFadeChg) // 95-only
            sw.WriteCommand("chan_stop_nofadechg");
        else
            sw.WriteCommand("chan_stop");
    }
}
