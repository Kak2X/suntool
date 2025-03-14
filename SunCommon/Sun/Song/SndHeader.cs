using SunCommon.Util;

namespace SunCommon;

public class SndHeader : IRomData
{
    public readonly List<SndChHeader> Channels = [];
    private string _name = null!;
    public string Name
    {
        get => (IsSfx ? "SFX_" : "BGM_") + _name;
        set
        {
            if (value.StartsWith("SFX_", StringComparison.OrdinalIgnoreCase) || value.StartsWith("BGM_", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(4);
            _name = LabelNormalizer.Apply(value);
        }
    }
    public int Id { get; set; }
    public bool IsSfx { get; set; }
    public int? ChannelCount { get; set; }

    public string? GetLabel() => $"SndHeader_{Name}";
    public int SizeInRom() => 1;
    public void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteIndent($"db {(ChannelCount ?? Channels.Count).AsHexByte()} ; Number of channels");
    }
}
