namespace SunCommon;

public class CmdDutyCycle : SndOpcode
{
    public int Duty { get; set; }
    public int Length { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var args = new List<string>(2) { $"{Duty}" };
        if (Length > 0)
            args.Add($"{Length}");
        sw.WriteCommand("duty_cycle", [.. args]);
    }
}
