using SunCommon;
namespace SunDis;

public class CmdVibratoOp : SndOpcode
{
    public readonly int VibratoId;

    public CmdVibratoOp(GbPtr p, Stream s) : base(p)
    {
        VibratoId = s.ReadByte() / 3;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("vibrato_on", $"${VibratoId:X2}");
    }
}
