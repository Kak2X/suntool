namespace SunCommon;

public class VibratoItem : IRomData
{
    public int Id { get; set; }
    public required byte[] Offsets { get; set; }
    public List<VibratoDef> Parents { get; } = [];

    public int SizeInRom() => Offsets.Length + 1;

    public string? GetLabel() => $"Sound_VibratoSet_{Id:X}_\\1";
    public void WriteToDisasm(IMultiWriter sw)
    {
        for (var i = 0; i < Offsets.Length; i++)
        {
            // Optimized labels (more than one possible)
            if (i > 0)
                for (var j = 0; j < Parents.Count; j++)
                    if (Parents[j].StartPoint == i)
                        sw.WriteLine($".sp{i:X}:");

            sw.WriteIndent($"db {Offsets[i].ToSigned()}");
        }
        sw.WriteIndent("db VIBCMD_LOOP");
    }
}
