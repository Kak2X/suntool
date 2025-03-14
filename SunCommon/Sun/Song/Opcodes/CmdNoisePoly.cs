namespace SunCommon;

public class CmdNoisePoly : SndOpcode, ICmdNote
{
    public SciNote Note { get; set; }
    public int? Length { get; set; }
    public override int SizeInRom() => 1 + CmdWait.MakeSize(Length);
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var args = new List<string>(2) { SciNote.ToNoteAsm(Note) };
        if (Length.HasValue)
            args.Add(CmdWait.Make(sw, Length.Value, true).TrimEnd());
        sw.WriteCommand("note4", [.. args]);
    }
}