namespace SunnyDay;

public class CmdDutyCycle : SndOpcode
{
    public readonly int Duty;
    public readonly int Length;

    public CmdDutyCycle(GbPtr p, Stream s) : base(p)
    {
        var x = s.ReadByte();
        Duty = (x >> 6) & 0b11;
        Length = x & 0b111111;
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var args = new List<string>(2) { $"{Duty}" };
        if (Length > 0)
            args.Add($"{Length}");
        sw.WriteCommand("duty_cycle", [.. args]);
    }
}
