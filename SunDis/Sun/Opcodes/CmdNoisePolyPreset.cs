using SunCommon;

namespace SunDis;

public class CmdNoisePolyPreset : SndOpcode, IMacroLength
{
    public readonly int PresetId;
    public int? Length { get; set; }

    private static readonly byte[][] Presets95 =
    [
        [0x00,0x00],
        [0x51,0x36],
        [0x52,0x24],
        [0x31,0x21],
        [0x53,0x11],
        [0x53,0x11],
        [0x53,0x11],
        [0x52,0x36],
        [0x52,0x36],
        [0x52,0x36],
    ];

    public CmdNoisePolyPreset(GbPtr p, int cmd) : base(p)
    {
        PresetId = cmd - 0x80;
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var args = new List<string>
        {
            $"${PresetId:X2}"
        };
        var cmd = "note4p";
        if (Length.HasValue)
            args.Add(Length.Value.ToString());

        args.Add($"; envelope ${Presets95[PresetId][0]:X2}");
        var noteId = Presets95[PresetId][1];
        if (CmdNoisePoly.TbmValues.Contains((byte)(noteId & 0xF7)))
            args.Add($"; note4 {SciNote.ToNoteAsm(SciNote.CreateFromNoise(noteId))}");
        else
            args.Add($"; note4x ${noteId:X2} ; Nearest: {SciNote.ToNoteAsm(SciNote.CreateFromNoise(noteId))}");

        sw.WriteCommand(cmd, [.. args]);
    }
}