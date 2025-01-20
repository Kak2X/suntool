using System.Diagnostics;
namespace SunnyDay;

public struct SciNote
{
    public readonly NoteDef Note;
    public readonly int Octave;

    public SciNote(NoteDef note, int octave)
    {
        Note = note;
        Octave = octave;
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
        return new SciNote(note, octave);
    }

    public static SciNote? CreateFromNoise(int rawValue)
    {
        Debug.Assert(rawValue >= 0 && rawValue <= 255);

        var noteTriplet = (rawValue >> 4) + 1;
        var noteMul = (rawValue & 7) < 4 ? 0 : ((noteTriplet % 3) * 4);
        var note = 11 - noteMul - (rawValue & 3);
        var octave = 6 - (noteTriplet / 3);

        return new SciNote((NoteDef)note, octave);
    }

    public static string ToNoteAsm(SciNote? note)
    {
        return note.HasValue ? $"{note.Value.Note.ToLabel()},{note.Value.Octave}" : "0";
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