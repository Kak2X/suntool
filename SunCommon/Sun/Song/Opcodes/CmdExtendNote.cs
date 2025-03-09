using System.Diagnostics;

namespace SunCommon;

public class CmdExtendNote : SndOpcode
{
    public int Length { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw) => sw.Write(Make(sw, Length));

    internal static string Make(IMultiWriter sw, int n)
    {
        var res = "";
        for (; n > 0; n -= 0xFF)
            res += sw.MakeCommand("continue", $"{Math.Min(n, 0xFF)}");
        return res;
    }
}
