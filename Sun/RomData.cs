namespace SunnyDay;

public abstract class RomData : IEquatable<RomData?>, IComparable
{
    public readonly GbPtr Location;
    
    /// <summary>
    ///     Size of the structure, WITHOUT any pointed entries.
    /// </summary>
    public abstract int SizeInRom();

    /// <summary>
    ///     Writes the data formatted for the disassembly. Should not include children entries.
    /// </summary>
    public abstract void WriteToDisasm(MultiWriter sw);

    public RomData(Stream s)
    {
        Location = GbPtrPool.Create(s.Position);
    }
    public RomData(GbPtr location)
    {
        Location = location;
    }

    public override bool Equals(object? obj) => Equals(obj as RomData);
    public bool Equals(RomData? other) => other is not null && Location.Equals(other.Location);
    public override int GetHashCode() => HashCode.Combine(Location);
    public int CompareTo(object? obj) => ((IComparable)Location).CompareTo((obj as RomData)?.Location);
    public static bool operator ==(RomData? left, RomData? right) => EqualityComparer<RomData>.Default.Equals(left, right);
    public static bool operator !=(RomData? left, RomData? right) => !(left == right);
}
