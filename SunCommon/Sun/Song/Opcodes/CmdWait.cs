using System.Diagnostics;

namespace SunCommon;

public class CmdWait : SndOpcode
{
    public int? Length { get; set; }
    public override int SizeInRom() => Length.HasValue ? 1 : 0;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        if (Length.HasValue) // Null when invalidated
            sw.Write(Make(sw, Length.Value, false));
    }

    internal static string Make(IMultiWriter sw, int n, bool firstInline)
    {
        var wait = (firstInline ? $"{Math.Min(n, 0x7F)}\r\n" : sw.MakeCommand("wait", $"{Math.Min(n, 0x7F)}"));
        var continues = CmdExtendNote.Make(sw, n - 0x7F);
        return wait + continues;
    }
}
