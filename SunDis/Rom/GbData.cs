using SunCommon;

namespace SunDis;

public class GbData : IEquatable<GbData?>, IComparable
{
    public GbData(GbPtr loc, IRomData data)
    {
        Location = loc;
        Data = data;
    }
    public GbPtr Location { get; }
    public IRomData Data { get; }

    #region RomAddress Equality
    public override bool Equals(object? obj) => Equals(obj as GbData);
    public bool Equals(GbData? other) => other is not null && Location.Equals(other.Location);
    public override int GetHashCode() => HashCode.Combine(Location);
    public int CompareTo(object? obj) => ((IComparable)Location).CompareTo((obj as GbData)?.Location);
    public static bool operator ==(GbData? left, GbData? right) => EqualityComparer<GbData>.Default.Equals(left, right);
    public static bool operator !=(GbData? left, GbData? right) => !(left == right);
    #endregion
}
