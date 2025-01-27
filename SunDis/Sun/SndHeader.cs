using SunCommon;
namespace SunDis;

public class SndHeader : RomData, IFileSplit
{
    public readonly List<SndChHeader> Channels;
    public readonly int Id;
    public readonly bool IsSfx;

    public SndHeader(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        var count = s.ReadByte();
        if (count > 4 || count <= 0)
            throw new InvalidSongHeaderException($"Song {song.Id:X2} at {Location.ToBankString()} probably points to code.");

        Channels = new List<SndChHeader>(count);
        for (var i = 0; i < count; i++)
            Channels.Add(new SndChHeader(s, song, opt));

        // IsSfx / TypeString is defined by here
        Id = song.Id;
        IsSfx = song.IsSfx;
        Location.Label = $"SndHeader_{song.TypeString}_{song.Id:X2}";
    }

    public string GetFilename()
    {
        var s = IsSfx ? "sfx" : "bgm";
        return $"{s}\\{s}_{Id:x2}.asm";
    }

    public override int SizeInRom()
    {
        return 1;
    }

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteIndent($"db ${Channels.Count:X2} ; Number of channels");
    }
}
