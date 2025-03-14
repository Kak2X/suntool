using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace SunCommon;

public static class NumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToSigned(this int n)
    {
        return n > 0x7F ? n - 0x100 : n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToSigned(this byte n) => ToSigned((int)n);

    public static string GenerateConstLabel<T>(this T n) where T : Enum
    {
        return n.ToString().Replace(", ", "|");
    }

    public static bool EndsWith([NotNull] this IList<byte> haystack, [NotNull] IList<byte> needle)
    {
        if (needle.Count > haystack.Count) return false;
        for (var i = 0; i < needle.Count; i++)
            if (needle[i] != haystack[haystack.Count - needle.Count + i])
                return false;
        return true;
    }
}

public static class FormatExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsHexByte(this int value)
    {
        return $"${value:X02}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsHexByte(this byte value) => AsHexByte((int)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsHexWord(this int value)
    {
        return $"${value:X04}";
    }

    public static string AsHexBytes(this byte[] value)
    {
        var res = new string[value.Length];
        for (var i = 0; i < value.Length; i++)
            res[i] = value[i].AsHexByte();
        return string.Join(",", res);
    }
}

public static class StreamExtensions
{
    public static int ReadUint8(this Stream s)
    {
        return s.ReadByte();
    }

    public static byte[] ReadUint8s(this Stream s, int count)
    {
        var res = new byte[count];
        s.Read(res, 0, res.Length);
        return res;
    }

    public static int ReadBiasedUint8(this Stream s)
    {
        return s.ReadByte() + 1;
    }

    public static char ReadChar(this Stream s)
    {
        return (char)s.ReadByte();
    }
    public static bool ReadBool(this Stream s)
    {
        return s.ReadByte() != 0;
    }
    public static int ReadUint16(this Stream s)
    {
        var buf = new byte[2];
        s.Read(buf, 0, 2);
        return (buf[1] << 8) + buf[0];
    }

    public static int[] ReadUint16s(this Stream s, int count)
    {
        var res = new int[count];
        for (var i = 0; i < count; i++)
            res[i] = s.ReadUint16();
        return res;
    }

    public static int ReadUint32(this Stream s)
    {
        var buf = new byte[4];
        s.Read(buf, 0, 4);
        return (buf[3] << 24) + (buf[2] << 16) + (buf[1] << 8) + buf[0];
    }
    public static string ReadLString(this Stream s)
    {
        var length = s.ReadUint16();
        var buf = new byte[length];
        s.Read(buf, 0, length);
        return Encoding.UTF8.GetString(buf);
    }
    public static string ReadString(this Stream s, int length)
    {
        var buf = new byte[length];
        s.Read(buf, 0, length);
        return Encoding.UTF8.GetString(buf);
    }
    public static float ReadFloat32(this Stream s)
    {
        var buf = new byte[4];
        s.Read(buf, 0, 4);
        return BitConverter.ToSingle(buf);
    }
}
