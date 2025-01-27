namespace SunDis;

public class InvalidSongHeaderException : Exception
{
    public InvalidSongHeaderException(string? message) : base(message)
    {
    }
}
