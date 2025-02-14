using SunCommon;

namespace SunDis;

public class CmdNoisePoly : SndOpcode
{
    public readonly SciNote? Note;
    public readonly int RawValue;
    public readonly int? Length;

    internal static readonly byte[] TbmValues = 
    [
        0xD7,0xD6,0xD5,0xD4,0xC7,0xC6,0xC5,0xC4,0xB7,0xB6,0xB5,0xB4,
        0xA7,0xA6,0xA5,0xA4,0x97,0x96,0x95,0x94,0x87,0x86,0x85,0x84,
        0x77,0x76,0x75,0x74,0x67,0x66,0x65,0x64,0x57,0x56,0x55,0x54,
        0x47,0x46,0x45,0x44,0x37,0x36,0x35,0x34,0x27,0x26,0x25,0x24,
        0x17,0x16,0x15,0x14,0x07,0x06,0x05,0x04,0x03,0x02,0x01,0x00,
    ];

    public CmdNoisePoly(GbPtr p, int cmd, Stream s) : base(p)
    {
        RawValue = cmd;
        if (TbmValues.Contains((byte)(cmd & 0xF7)))
            Note = SciNote.CreateFromNoise(cmd);
        Length = s.ReadUint8();
        if (Length > 0x7F)
        {
            Length = null;
            s.Seek(-1, SeekOrigin.Current);
        }
    }

    public override int SizeInRom() => 1 + (Length.HasValue? 1 : 0);

    public override void WriteToDisasm(MultiWriter sw)
    {
        var args = new List<string>();
        string cmd;
        if (Note != null)
        {
            cmd = "note4";
            args.Add(SciNote.ToNoteAsm(Note));
        }
        else
        {
            cmd = "note4x";
            args.Add($"${RawValue:X2}");
        }
        if (Length.HasValue)
            args.Add(Length.Value.ToString());
        if (Note == null)
            args.Add("; Nearest: " + SciNote.ToNoteAsm(SciNote.CreateFromNoise(RawValue)));

        sw.WriteCommand(cmd, [.. args]);
    }
}
