using System.Globalization;
namespace SunnyDay;

public class GbPtr : IEquatable<GbPtr>, IComparable, IComparable<GbPtr>
{
    public readonly int Bank;
    public readonly int Address;
    public readonly long RomAddress;
    public string? Label { get; set; }

    private const int BankSize = 0x4000;

    public GbPtr(int bank, int address)
    {
        Bank = bank;
        Address = address;
        RomAddress = (bank * BankSize) + (address >= BankSize ? address - BankSize : address);
    }

    public GbPtr(long romAddress)
    {
        Bank = (int)(romAddress / BankSize);
        Address = (int)(romAddress % BankSize) + (Bank > 0 ? BankSize : 0);
        RomAddress = romAddress;
    }

    public bool SetLabelIfNew(string v)
    {
        if (Label != null)
            return false;
        Label = v;
        return true;
    }

    public string ToBankString() => $"{Bank:X2}:{Address:X4}";
    public string ToDefaultLabel() => $"L{Bank:X2}{Address:X4}";
    public string ToLabel() => Label ?? ToDefaultLabel();

    public static GbPtr Create(string loc)
    {
        var x = loc.Split(':');
        return x.Length switch
        {
            2 => new GbPtr(int.Parse(x[0], NumberStyles.HexNumber), int.Parse(x[1], NumberStyles.HexNumber)),
            1 => new GbPtr(int.Parse(x[0], NumberStyles.HexNumber)),
            _ => throw new ArgumentException($"Pointer '{loc}' isn't valid.", nameof(loc))
        };
    }

    public static int GetBank(long romAddr) => (int)(romAddr / BankSize);


    #region RomAddress Equality
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        else if (obj is null)
            return false;
        else
            return obj is GbPtr ptr && Equals(ptr);
    }

    public override int GetHashCode() => HashCode.Combine(RomAddress);
    public bool Equals(GbPtr? other) => other is not null && RomAddress == other.RomAddress;
    public int CompareTo(GbPtr? other) => RomAddress.CompareTo(other?.RomAddress ?? -1);
    public int CompareTo(object? obj) => RomAddress.CompareTo((obj as GbPtr)?.RomAddress ?? -1);
    public static bool operator ==(GbPtr left, GbPtr right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(GbPtr left, GbPtr right) => !(left == right);
    public static bool operator <(GbPtr left, GbPtr right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(GbPtr left, GbPtr right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(GbPtr left, GbPtr right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator >=(GbPtr left, GbPtr right) => left is null ? right is null : left.CompareTo(right) >= 0;
    #endregion
}
