namespace SunCommon;

public class CmdNoisePolyCustom : SndOpcode
{
    public int RawValue { get; set; }
    public int? Length { get; set; }
    public override int SizeInRom() => 1 + (Length.HasValue ? 1 : 0);
    public override void WriteToDisasm(IMultiWriter sw)
    {
        var args = new List<string>(3) { RawValue.AsHexByte() };
        if (Length.HasValue)
            args.Add(Length.Value.ToString());
        args.Add("; Nearest: " + SciNote.ToNoteAsm(SciNote.CreateFromNoise(RawValue)));
        sw.WriteCommand("note4x", [.. args]);
    }
}