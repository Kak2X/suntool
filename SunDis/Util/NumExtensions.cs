namespace SunDis;

public static class NumExtensions
{
    public static int ToSigned(this int n)
    {
        return n > 0x7F ? n - 0x100 : n;
    }

    public static string GenerateConstLabel<T>(this T n) where T : Enum
    {
        return n.ToString().Replace(", ", "|");
    }
}