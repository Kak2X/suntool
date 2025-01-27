using SunCommon;
using System.Diagnostics;
namespace SunDis;

public class OpcodeDump(SortedSet<RomData> opcodes)
{
    public void ToDisassembly(string path)
    {
        // Just in case, make sure opcodes that refer to pointers
        var importantPtrs = new HashSet<GbPtr>();
        foreach (var x in opcodes.OfType<IHasPointer>())
            importantPtrs.Add(x.Target);

        using (var w = new MultiWriter(path))
        {
            // Split when crossing over lines
            Debug.Assert(opcodes.First() is IFileSplit, "This shouldn't happen, exception incoming.");
            foreach (var opcode in opcodes)
            {
                // Split point
                if (opcode is IFileSplit splitable)
                    w.ChangeFile(splitable.GetFilename());
                if (opcode.Location.Label != null || importantPtrs.Contains(opcode.Location))
                    w.WriteLine($"{opcode.Location.ToLabel()}:");
                opcode.WriteToDisasm(w);
            }

            w.ChangeFile("main.asm", false);
            foreach (var x in w.FileHistory)
                w.WriteLine($"INCLUDE \"{x.Replace('\\', '/')}\"");
        }
    }
}
