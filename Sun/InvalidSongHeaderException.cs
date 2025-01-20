namespace SunnyDay;

public class InvalidSongHeaderException : Exception
{
    public InvalidSongHeaderException(string? message) : base(message)
    {
    }
}
