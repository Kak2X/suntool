using SunCommon;
namespace SunDis;

public class CmdSpeed : SndOpcode // 95-only
{
    public readonly int Arg;

    public CmdSpeed(GbPtr p, Stream s) : base(p)
    {
        Arg = s.ReadUint16();
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("speed", $"${Arg:X4}");
    }
}
