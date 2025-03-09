namespace SunCommon;

public class CmdWaveCutoff : SndOpcode
{
    public int Length { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("wave_cutoff", $"{Length}");
    }
}
