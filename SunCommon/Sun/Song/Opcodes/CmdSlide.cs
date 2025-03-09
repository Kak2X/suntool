namespace SunCommon;

public class CmdSlide : SndOpcode
{
    public int Length { get; set; }
    public SciNote? Note { get; set; }
    public override int SizeInRom() => 3;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("pitch_slide", SciNote.ToNoteAsm(Note), $"{Length}");
    }
}
