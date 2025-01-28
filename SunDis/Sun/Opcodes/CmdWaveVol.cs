using SunCommon;
namespace SunDis;

public class CmdWaveVol : SndOpcode
{
    public readonly int Vol;

    public CmdWaveVol(GbPtr p, Stream s) : base(p)
    {
        // Normalized, making it comparable to CndEnv
        Vol = (((s.ReadByte() >> 5) - 1 ^ 0xFF) & 3) << 6;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wave_vol", $"${Vol:X2}");
    }
}
