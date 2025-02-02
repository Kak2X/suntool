using SunCommon;
namespace SunDis;

public class CmdExtendNote : SndOpcode
{
    public readonly int Length;

    public CmdExtendNote(GbPtr p, Stream s) : base(p)
    {
        Length = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("continue", $"{Length}");
    }
}
