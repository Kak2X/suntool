namespace SunCommon;

public class WaveTable : IRomData
{
    private readonly List<WaveItem> Waves = [];
    public int SizeInRom() => Waves.Count * 2;
    public string? GetLabel() => null;
    public void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteLine(Consts.WaveTblBegin);
        foreach (var x in Waves)
            sw.WriteIndent($"dw {x.GetLabel()}");
        foreach (var x in Waves)
            sw.WriteWithLabel(x);    
    }

    public WaveItem this[int index] => Waves[index];
    public int Push(byte[] data, string? name = null)
    {
        if (data.Length != 0x10) 
            throw new ArgumentException("Wave data must be 0x10 bytes long", nameof(data));
        // Try find existing
        for (var i = 0; i < Waves.Count; i++)
            if (data.SequenceEqual(Waves[i].Data))
                return i;
        // Add if not found
        var toAdd = new WaveItem { Id = Waves.Count, Data = data, Name = name };
        Waves.Add(toAdd);

        return toAdd.Id;
    }
}
