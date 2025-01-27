namespace SunDis;

public class CmdClrVibrato : SndOpcode
{
    public CmdClrVibrato(GbPtr p) : base(p)
    {
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("vibrato_off");
    }
}
