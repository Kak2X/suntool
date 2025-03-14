namespace SunCommon;

public class CmdNoisePolyCustom : SndOpcode, ICmdNote
{
    public int RawValue { get; set; }
    public int? Length { get; set; }
    public override int SizeInRom() => 1 + CmdWait.MakeSize(Length);
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var args = new List<string>(3) { RawValue.AsHexByte() };
        if (Length.HasValue)
            args.Add(CmdWait.Make(sw, Length.Value, true).TrimEnd());
        args.Add("; Nearest: " + SciNote.ToNoteAsm(SciNote.CreateFromNoise(RawValue)));
        sw.WriteCommand("note4x", [.. args]);
    }
}