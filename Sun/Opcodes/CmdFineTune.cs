namespace SunnyDay;

public class CmdFineTune : SndOpcode
{
    public readonly int Offset;

    public CmdFineTune(GbPtr p, Stream s) : base(p)
    {
        Offset = s.ReadByte();
    }

    public override int SizeInRom() => 2;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("fine_tune", $"{Offset.ToSigned()}");
    }
}
