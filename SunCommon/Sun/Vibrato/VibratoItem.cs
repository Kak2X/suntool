namespace SunCommon;

public class VibratoItem : IRomData
{
    public List<VibratoDef> Parents { get; } = [];
    public required byte[] Offsets { get; set; }

    public int SizeInRom() => Offsets.Length + 1;

    public string? GetLabel() => $"Sound_VibratoSet{Parents[0].Id}_\\1";
    public void WriteToDisasm(IMultiWriter sw)
    {
        for (var i = 0; i < Offsets.Length; i++)
        {
            // Optimized labels (more than one possible)
            for (var j = 0; j < Parents.Count; j++)
                if (Parents[j].StartPoint == i)
                    sw.WriteLine($"Sound_VibratoSet{Parents[j].Id}_\\1:");

            sw.WriteIndent($"db {Offsets[i].ToSigned()}");
        }
        sw.WriteIndent("db VIBCMD_LOOP");
    }
}
