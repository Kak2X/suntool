namespace SunnyDay;

public static class OpcodeParser
{
    public static IOpcodeParser Create(FormatOptions opt)
    {
        return opt.Mode == DataMode.KOF96
            ? new OpcodeParser96()
            : new OpcodeParserOp();
    }
}
