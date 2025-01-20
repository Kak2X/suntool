namespace SunnyDay;

public class CmdEnaCh : SndOpcode
{
    public readonly int Pan; // Nr51

    public CmdEnaCh(GbPtr p, Stream s) : base(p)
    {
        Pan = s.ReadByte(); // (Nr51)
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("panning", $"${Pan:X2}"); // Pan.GenerateConstLabel());
    }
}
