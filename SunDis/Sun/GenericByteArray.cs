namespace SunDis;

public class GenericByteArray : RomData, IFileSplit
{
    public readonly byte[] Data;

    public GenericByteArray(Stream s, long targetPos) : base(s)
    {
        Location.Label = $"Padding_{Location.RomAddress:X8}";
        var length = (int)(targetPos - s.Position);
        Data = new byte[length];
        s.Read(Data, 0, length);
    }

    public string GetFilename() => $"padding/{Location.ToDefaultLabel()}.asm";

    public override int SizeInRom() => Data.Length;

    public override void WriteToDisasm(MultiWriter sw)
    {
        foreach (var x in Data)
            sw.WriteIndent($"db ${x:X2}");
    }
}
