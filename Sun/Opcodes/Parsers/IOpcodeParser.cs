namespace SunnyDay;

public interface IOpcodeParser
{
    public SndOpcode Parse(Stream s, SongInfo song);
}
