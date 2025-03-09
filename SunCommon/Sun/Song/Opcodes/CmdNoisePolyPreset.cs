namespace SunCommon;

public class CmdNoisePolyPreset : SndOpcode, IMacroLength
{
    public int PresetId { get; set; }
    public int? Length { get; set; }
    public override int SizeInRom() => 1 + (Length.HasValue ? 1 : 0);
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var args = new List<string>
        {
            PresetId.AsHexByte()
        };
        if (Length.HasValue)
            args.Add(CmdWait.Make(sw, Length.Value, true).TrimEnd());

        args.Add($"; envelope ${Consts.NotePresets95[PresetId][0]:X2}");
        var noteId = Consts.NotePresets95[PresetId][1];
        if (Consts.TbmNoiseNotes.Contains((byte)(noteId & 0xF7)))
            args.Add($"; note4 {SciNote.ToNoteAsm(SciNote.CreateFromNoise(noteId))}");
        else
            args.Add($"; note4x {noteId.AsHexByte()} ; Nearest: {SciNote.ToNoteAsm(SciNote.CreateFromNoise(noteId))}");

        sw.WriteCommand("note4p", [.. args]);
    }
}