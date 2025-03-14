using System.Diagnostics;
namespace SunCommon;

public struct SciNote
{
    public readonly NoteDef Note;
    public readonly int Octave;
    public readonly int? Lfsr;

    public static readonly byte[] NoiseNoteTable = [
        0xD7,  0xD6,  0xD5,  0xD4,  0xC7,  0xC6,  0xC5,  0xC4,  0xB7,  0xB6,  0xB5,  0xB4,
        0xA7,  0xA6,  0xA5,  0xA4,  0x97,  0x96,  0x95,  0x94,  0x87,  0x86,  0x85,  0x84,
        0x77,  0x76,  0x75,  0x74,  0x67,  0x66,  0x65,  0x64,  0x57,  0x56,  0x55,  0x54,
        0x47,  0x46,  0x45,  0x44,  0x37,  0x36,  0x35,  0x34,  0x27,  0x26,  0x25,  0x24,
        0x17,  0x16,  0x15,  0x14,  0x07,  0x06,  0x05,  0x04,  0x03,  0x02,  0x01,  0x00
    ];

    public SciNote(NoteDef note, int octave, int? lfsr)
    {
        Note = note;
        Octave = octave;
        Lfsr = lfsr;
    }

    public static SciNote? Create(int rawValue)
    {
        Debug.Assert(rawValue >= 0 && rawValue <= 255);
        if (rawValue == 0) return null;

        NoteDef note;
        if (rawValue > 0x7F)
        {
            rawValue = rawValue - 0x100;
            note = (NoteDef)(11 + ((rawValue + 1) % 12));
        }
        else
        {
            rawValue--;
            note = (NoteDef)(rawValue % 12);
        }

        var octave = 2 + (int)Math.Floor(rawValue / (decimal)12);
        return new SciNote(note, octave, null);
    }

    public static SciNote CreateFromNoise(int rawValue)
    {
        Debug.Assert(rawValue >= 0 && rawValue <= 255);

        var lfsr = (rawValue & 0x08) >> 3;
        var noteTriplet = (rawValue >> 4) + 1;
        var noteMul = (rawValue & 7) < 4 ? 0 : ((noteTriplet % 3) * 4);
        var note = 11 - noteMul - (rawValue & 3);
        var octave = 6 - (noteTriplet / 3);

        return new SciNote((NoteDef)note, octave, lfsr);
    }

    public static string ToNoteAsm(SciNote? note)
    {
        return note.HasValue ? $"{note.Value.Note.ToLabel()},{note.Value.Octave}{(note.Value.Lfsr.HasValue ? $",{note.Value.Lfsr}" : "")}" : "0";
    }
}

public enum NoteDef
{
    C = 0,
    Ch = 1,
    D = 2,
    Dh = 3,
    E = 4,
    F = 5,
    Fh = 6,
    G = 7,
    Gh = 8,
    A = 9,
    Ah = 10,
    B = 11,
}

public static class NoteDefExtensions
{
    public static string ToLabel(this NoteDef x) => x switch
    {
        NoteDef.C => "C_",
        NoteDef.Ch => "C#",
        NoteDef.D => "D_",
        NoteDef.Dh => "D#",
        NoteDef.E => "E_",
        NoteDef.F => "F_",
        NoteDef.Fh => "F#",
        NoteDef.G => "G_",
        NoteDef.Gh => "G#",
        NoteDef.A => "A_",
        NoteDef.Ah => "A#",
        NoteDef.B => "B_",
        _ => throw new ArgumentOutOfRangeException(nameof(x)),
    };
}