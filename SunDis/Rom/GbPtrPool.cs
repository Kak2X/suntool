namespace SunDis;

// Enforces receiving the identical GbPtr object for the same RomAddress.
// This allows editing labels from the other side, useful when handling call and loop commands.
// Shouldn't be static but oh well...
public static class GbPtrPool
{
    private static readonly HashSet<GbPtr> _cache = [];
    public static void Clear() => _cache.Clear();
    public static GbPtr Create(int bank, int address) => GetCached(new GbPtr(bank, address));
    public static GbPtr Create(long romAddress) => GetCached(new GbPtr(romAddress));
    public static GbPtr Create(string loc) => GetCached(GbPtr.Create(loc));
    private static GbPtr GetCached(GbPtr ptr)
    {
        if (_cache.TryGetValue(ptr, out var found))
        {
            return found;
        }
        else
        {
            _cache.Add(ptr);
            return ptr;
        }
    }
}
