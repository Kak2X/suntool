namespace SunCommon;

public class SndChHeader : IRomData
{
    public SndHeader Parent { get; set; } = null!;
    public SndInfoStatus InitialStatus { get; set; }
    public SndChPtrNum SoundChannelPtr { get; set; }
    public SndData? Data { get; set; }
    public int FineTune { get; set; }
    public int Unused { get; set; }
    public bool IsUnused { get; set; }

    public SndChHeader()
    {
        Data = new() { Parent = this };
    }

    public int SizeInRom() => 6;
    public string? GetLabel() => $".{(IsUnused ? "unused_" : "")}ch{SoundChannelPtr.Normalize()}";
    public void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteIndent($"db {InitialStatus.GenerateConstLabel()} ; Initial playback status");
        sw.WriteIndent($"db {SoundChannelPtr} ; Sound channel ptr");
        if (Data == null)
            sw.WriteIndent($"dw {0.AsHexWord()} ; Data ptr");
        else
            sw.WriteIndent($"dw {Data.Main.Opcodes[0].GetLabel()} ; Data ptr");
        sw.WriteIndent($"db {FineTune.ToSigned()} ; Initial fine tune");
        sw.WriteIndent($"db {Unused.AsHexByte()} ; Initial vibrato ID");
    }
}
