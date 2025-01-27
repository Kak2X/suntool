using SunCommon;
namespace SunDis;

public class CmdSlide : SndOpcode
{
    public readonly int Length;
    public readonly SciNote? Note;

    public CmdSlide(GbPtr p, Stream s) : base(p)
    {
        Length = s.ReadByte();
        Note = SciNote.Create(s.ReadByte());
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("pitch_slide", SciNote.ToNoteAsm(Note), $"{Length}");
    }
}
