namespace SunCommon;

public class CmdWave : SndOpcode
{
    public int WaveId { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("wave_id", WaveId.AsHexByte());
    }
}
