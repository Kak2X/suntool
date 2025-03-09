namespace SunCommon;

public class CmdWaveVol : SndOpcode
{
    public int Vol { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("wave_vol", Vol.AsHexByte());
    }
}
