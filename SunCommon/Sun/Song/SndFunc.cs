﻿using System.Runtime.CompilerServices;

namespace SunCommon;

public class SndFunc : IRomData
{
    public SndData Parent { get; set; } = null!;
    public List<SndOpcode> Opcodes { get; } = [];
    internal int? SubId { get; set; }
    public bool IsUnused { get; set; }
    public string? GetLabel() => null;
    public int SizeInRom() => 0;
    public void WriteToDisasm(IMultiWriter sw) {}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOpcode(SndOpcode op)
    {
        op.Parent = this;
        Opcodes.Add(op);
    }
}
