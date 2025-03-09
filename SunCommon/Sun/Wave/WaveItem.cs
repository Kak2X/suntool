namespace SunCommon;

public class WaveItem : IRomData
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public required byte[] Data { get; set; }
    public int SizeInRom() => 0x10;
    public string? GetLabel() => $"Sound_WaveSet{Id}_\\1";
    public void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteIndent($"db {Data.AsHexBytes()} ; {Id.AsHexByte()} ; {Name}");
    }
}
