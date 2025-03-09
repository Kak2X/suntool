namespace SunCommon;

public class VibratoDef : IRomData
{
    public int Id { get; set; }
    public required VibratoItem Data { get; set; }
    public int StartPoint { get; set; }
    public int LoopPoint { get; set; }

    public int SizeInRom() => 3;
    public string? GetLabel() => null;
    public void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteIndent($"mVbDef {Data.GetLabel()}, {LoopPoint.AsHexByte()} ; {Id.AsHexByte()}");
    }
}
