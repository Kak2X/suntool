﻿namespace SunCommon;

public class CmdEnaCh : SndOpcode
{
    public int Pan { get; set; }
    public override int SizeInRom() => 2;
    public override void WriteToDisasm(IMultiWriter sw)
    {
        sw.WriteCommand("panning", Pan.AsHexByte());
    }
}
