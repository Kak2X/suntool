using SunCommon;
namespace SunDis;

public class CmdWave : SndOpcode
{
    public readonly int WaveId;

    public CmdWave(GbPtr p, Stream s) : base(p)
    {
        WaveId = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wave_id", $"${WaveId:X2}");
    }
}
