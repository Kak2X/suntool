using SunCommon;

namespace SunDis;

public static class StreamExtensions
{
    public static GbPtr ReadLocalPtr(this Stream s)
    {
        var bank = GbPtr.GetBank(s.Position);
        var ptr = s.ReadUint16();
        return GbPtrPool.Create(bank, ptr);
    }

    public static GbPtr ReadFarPtr(this Stream s)
    {
        var bank = s.ReadUint8();
        var ptr = s.ReadUint16();
        return GbPtrPool.Create(bank, ptr);
    }
}
