namespace SunDis;

public class CmdVibrato96 : SndOpcode
{
    public CmdVibrato96(GbPtr p) : base(p)
    {
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("vibrato");
    }
}
