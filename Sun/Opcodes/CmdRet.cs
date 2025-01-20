namespace SunnyDay;

public class CmdRet : SndOpcode
{
    public CmdRet(GbPtr p) : base(p)
    {
        Terminates = true;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_ret");
    }
}
