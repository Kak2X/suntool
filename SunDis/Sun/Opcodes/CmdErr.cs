using SunCommon;
namespace SunDis;

public class CmdErr : SndOpcode
{
    public readonly int Cmd;

    public CmdErr(GbPtr p, int cmd) : base(p)
    {
        Cmd = cmd;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snderr", $"${Cmd:X2}");
    }
}
