namespace SunCommon;

public class CmdChanStop : SndOpcode
{
    public CmdChanStop()
    {
        Terminates = true;
    }
    public PriorityGroup? Priority { get; set; }
    public bool NoFadeChg { get; set; }
    public override int SizeInRom() => 1;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        if (Priority.HasValue)
            sw.WriteCommand("chan_stop", Priority.Value.ToString());
        else if (NoFadeChg) // 95-only
            sw.WriteCommand("chan_stop_nofadechg");
        else
            sw.WriteCommand("chan_stop");
    }
}

[Flags]
public enum PriorityGroup
{
    SNP_SFXMULTI = 1 << 7,
    SNP_SFX4 = 1 << 6,
}
