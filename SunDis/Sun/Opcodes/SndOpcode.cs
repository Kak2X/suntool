namespace SunDis;

public abstract class SndOpcode : RomData
{
    public bool Terminates { get; init; }

    public SndOpcode(GbPtr p) : base(p)
    {
    }
}
