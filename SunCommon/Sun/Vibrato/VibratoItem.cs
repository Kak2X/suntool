﻿namespace SunCommon;

public class VibratoItem : IRomData
{
    public int Id { get; set; }
    public required byte[] Offsets { get; set; }

    public int SizeInRom() => Offsets.Length + 1;

    public string? GetLabel() => $"Sound_VibratoSet_{Id:X}_\\1";
    public void WriteToDisasm(IMultiWriter sw)
    {
        for (var i = 0; i < Offsets.Length; i++)
            sw.WriteIndent($"db {Offsets[i].ToSigned()}");
        sw.WriteIndent("db VIBCMD_LOOP");
    }
}
