using SunCommon;
using System.Diagnostics;
using static SunCommon.Sun.Consts;
namespace SunDis;

public class OpcodeDump(PointerTable origin, SortedSet<RomData> opcodes)
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
            if (opcodes.First() is not IFileSplit)
                w.ChangeFile("driver/padding/initial.asm");

            foreach (var opcode in opcodes)
            {
                // Split point
                if (opcode is IFileSplit splittable)
                    w.ChangeFile(splittable.GetFilename());
                if (opcode.Location.Label != null || importantPtrs.Contains(opcode.Location))
                    w.WriteLine($"{opcode.Location.ToLabel()}:");
                opcode.WriteToDisasm(w);
            }

            // Autodetect the init code
            w.ChangeFile("driver/data/song_list.asm");
            w.Write(SndListBegin);
            foreach (var song in origin.Songs)
            {
                string initCode;
                if (song.Id == 0x0C)
                    initCode = SndInitPause;
                else if (song.Id == 0x0D)
                    initCode = SndInitUnpause;
                else if (!song.IsSfx)
                    initCode = SndInitNewBgm;
                else initCode = song.Channels.Min(x => x.SoundChannelPtr) switch
                {
                    SndChPtrNum.SND_CH1_PTR => SndInitNewSfx1234,
                    SndChPtrNum.SND_CH2_PTR or SndChPtrNum.SND_CH3_PTR => SndInitNewSfx234,
                    SndChPtrNum.SND_CH4_PTR => SndInitNewSfx4,
                    _ => SndInitDummy,
                };
                w.WriteLine($"\tdsong {song.Location.ToLabel()}, {initCode}_\\1 ; ${(song.Id + 0x80):X02}");
            }
            w.WriteLine($".end:");


            w.ChangeFile("driver/main.asm", false);
            foreach (var x in w.FileHistory)
                w.WriteLine($"INCLUDE \"{x.Replace('\\', '/')}\"");
        }
    }
}
