namespace SunCommon;

public class CmdNoisePoly : SndOpcode
{
    public SciNote Note { get; set; }
    public int? Length { get; set; }
    public override int SizeInRom() => 1 + (Length.HasValue? 1 : 0);
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var args = new List<string>(2) { SciNote.ToNoteAsm(Note) };
        if (Length.HasValue)
            args.Add(Length.Value.ToString());
        sw.WriteCommand("note4", [.. args]);
    }
}