namespace SunDis;

public interface IOpcodeParser
{
    public SndOpcode Parse(Stream s, SongInfo song);
}
