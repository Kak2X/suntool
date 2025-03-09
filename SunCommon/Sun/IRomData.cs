using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SunCommon;

public interface IRomData
{
    /// <summary>
    ///     Size of the structure, WITHOUT any pointed entries.
    /// </summary>
    int SizeInRom();

    /// <summary>
    ///     Writes the data formatted for the disassembly. Should not include children entries.
    /// </summary>
    void WriteToDisasm(IMultiWriter sw);

    /// <summary>
    ///     Gets the label associated with the structure.
    /// </summary>
    /// <returns></returns>
    string? GetLabel();
}

public static class IRomDataExtensions
{
    public static void WriteWithLabel(this IMultiWriter sw, IRomData obj)
    {
        var lbl = obj.GetLabel();
        if (lbl != null)
            sw.WriteLine($"{lbl}:");
        obj.WriteToDisasm(sw);
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("DEBUG")]
    public static void EnsureSet([NotNull] this IRomData? obj)
    {
        if (obj == null)
            throw new InvalidOperationException("You forgot to assign this entity.");
    }
}