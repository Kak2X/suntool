namespace SunnyDay;

public class CmdWaveVol : SndOpcode
{
    public readonly int Vol;

    public CmdWaveVol(GbPtr p, Stream s) : base(p)
    {
        // Normalized, making it comparable to CndEnv
        Vol = s.ReadByte() << 1;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("wave_vol", $"${Vol:X2}");
    }
}
