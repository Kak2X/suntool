namespace SunCommon;

public class SndData : IRomData
{
    public SndChHeader Parent { get; set; } = null!;
    public SndFunc Main { get; }
    public List<SndFunc> Subs { get; } = [];
    public SndData()
    {
        Main = new() { Parent = this };
    }
    public int SizeInRom() => 0;
    public string? GetLabel() => null;
    public void WriteToDisasm(IMultiWriter sw) {}
}
