namespace SunCommon;

public enum SndChPtrNum
{
    SND_CH1_PTR = 0x13,
    SND_CH2_PTR = 0x18,
    SND_CH3_PTR = 0x1D,
    SND_CH4_PTR = 0x22,
}
public static class SndChPtrNumExtensions
{
    public static int Normalize(this SndChPtrNum value)
    {
        return value switch
        {
            SndChPtrNum.SND_CH1_PTR => 1,
            SndChPtrNum.SND_CH2_PTR => 2,
            SndChPtrNum.SND_CH3_PTR => 3,
            SndChPtrNum.SND_CH4_PTR => 4,
            _ => (int)value,
        };
    }

    public static SndChPtrNum Next(this SndChPtrNum value)
    {
        return value switch
        {
            SndChPtrNum.SND_CH1_PTR => SndChPtrNum.SND_CH2_PTR,
            SndChPtrNum.SND_CH2_PTR => SndChPtrNum.SND_CH3_PTR,
            SndChPtrNum.SND_CH3_PTR => SndChPtrNum.SND_CH4_PTR,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "SndChPtrNum.Next on noise."),
        };
    }
}