namespace SunnyDay;

public static class StreamExtensions
{
    public static byte[] ReadBytes(this Stream s, int count)
    {
        var res = new byte[count];
        s.Read(res, 0, res.Length);
        return res;
    }

    public static int[] ReadWords(this Stream s, int count)
    {
        var res = new int[count];
        for (var i = 0; i < count; i++)
            res[i] = s.ReadWord();
        return res;
    }

    public static int ReadWord(this Stream s)
    {
        var lo = s.ReadByte();
        var hi = s.ReadByte();
        return (hi << 8) + lo;
    }

    public static GbPtr ReadLocalPtr(this Stream s)
    {
        var bank = GbPtr.GetBank(s.Position);
        var ptr = s.ReadWord();
        return GbPtrPool.Create(bank, ptr);
    }
}
