using System.Diagnostics;

namespace SunCommon;

public class CmdExtendNote : SndOpcode, ICmdNote
{
    public int? Length { get; set; }
    public override int SizeInRom() => MakeSize(Length.GetValueOrDefault());
    public override void WriteToDisasm(IMultiWriter sw) => sw.Write(Make(sw, Length.GetValueOrDefault()));

    internal static string Make(IMultiWriter sw, int n)
    {
        var res = "";
        for (; n > 0; n -= 0xFF)
            res += sw.MakeCommand("continue", $"{Math.Min(n, 0xFF)}");
        return res;
    }

    internal static int MakeSize(int length) => 2 * (int)Math.Ceiling(length / (decimal)0xFF);
}
