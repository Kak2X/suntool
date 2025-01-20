namespace SunnyDay;

public class CmdNote : SndOpcode, IMacroLength
{
    public readonly SciNote? Note;
    public int? Length { get; set; }

    public CmdNote(GbPtr p, int cmd) : base(p)
    {
        Note = SciNote.Create(cmd - (int)SndCmdType.SNDNOTE_BASE);
    }

    public override int SizeInRom() => 1;

    public override void WriteToDisasm(MultiWriter sw)
    {
        var cmd = Note == null ? "silence" : "note";
        var args = new List<string>();
        if (Note != null)
            args.Add(SciNote.ToNoteAsm(Note));
        if (Length.HasValue)
            args.Add(Length.Value.ToString());

        sw.WriteCommand(cmd, [.. args]);
    }
}
