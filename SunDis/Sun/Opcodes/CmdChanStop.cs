using SunCommon;
namespace SunDis;

public class CmdChanStop : SndOpcode
{
    public readonly PriorityGroup? Priority;
    public CmdChanStop(GbPtr p, PriorityGroup? priority = null) : base(p)
    {
        Terminates = true;
        Priority = priority;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        if (Priority.HasValue)
            sw.WriteCommand("chan_stop", Priority.Value.ToString());
        else
            sw.WriteCommand("chan_stop");
    }
}
