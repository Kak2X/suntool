namespace SunDis;

public static class OpcodeParser
{
    public static IOpcodeParser Create(FormatOptions opt) => opt.Mode switch
    {
        DataMode.KOF95 => new OpcodeParser95(),
        DataMode.KOF96 => new OpcodeParser96(),
        DataMode.OP => new OpcodeParserOp(),
        _ => throw new ArgumentOutOfRangeException(nameof(opt)),
    };
}
