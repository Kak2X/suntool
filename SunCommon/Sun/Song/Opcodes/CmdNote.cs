namespace SunCommon;

public class CmdNote : SndOpcode, IMacroLength
{
    public SciNote? Note { get; set; }
    public int? Length { get; set; }
    public override int SizeInRom() => 1 + (Length.HasValue ? 1 : 0);
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var cmd = Note == null ? "silence" : "note";
        var args = new List<string>();
        if (Note != null)
            args.Add(SciNote.ToNoteAsm(Note));
        if (Length.HasValue)
            args.Add(CmdWait.Make(sw, Length.Value, true).TrimEnd());

        sw.WriteCommand(cmd, [.. args]);
    }
}
