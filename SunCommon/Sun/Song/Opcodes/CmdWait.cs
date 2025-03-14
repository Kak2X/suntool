using System.Diagnostics;

namespace SunCommon;

public class CmdWait : SndOpcode, ICmdNote
{
    public int? Length { get; set; }
    public override int SizeInRom() => MakeSize(Length);
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

    internal static int MakeSize(int? length)
    {
        if (!length.HasValue)
            return 0;
        else if (length <= 0x7F)
            return 1;
        else
            return 1 + (CmdExtendNote.MakeSize(length.Value - 0x7F));
    }

}
