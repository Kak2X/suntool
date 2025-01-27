using SunCommon;
namespace SunDis;

public class SndData : RomData
{
    public readonly SndFunc Main;
    public readonly List<SndFunc> Subs = [];

    public SndData(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        Location.SetLabelIfNew($"SndData_{song.TypeString}_{song.Id:X2}_Ch{song.ChNum}");

        Main = new SndFunc(s, song, opt);
        // Find calls from main function
        var uniqueCalls = new HashSet<GbPtr>();
        foreach (var x in Main.Opcodes.OfType<CmdCall>())
            uniqueCalls.Add(x.Target);

        // Traverse nested subroutines.
        // This uses a second buffer to store newly found unique nested calls,
        // which is swapped at the end with the processed ones.
        var toProc = uniqueCalls.ToList();
        var toProcNext = new List<GbPtr>();
        do
        {
            toProcNext.Clear();
            foreach (var call in toProc)
            {
                //--
                s.Seek(call.RomAddress, SeekOrigin.Begin);
                var sub = new SndFunc(s, song, opt);
                foreach (var x in sub.Opcodes.OfType<CmdCall>())
                    if (uniqueCalls.Add(x.Target))
                        toProcNext.Add(x.Target); // "recursion"
                Subs.Add(sub);
                //--
            }
            (toProc, toProcNext) = (toProcNext, toProc);
        } while (toProc.Count > 0); // after swap

        // Label the subroutines sequentially, by ROM order
        var subNo = 0;
        foreach (var x in Subs.OrderBy(x => x.Location))
        {
            x.Location.SetLabelIfNew($"SndCall_{song.TypeString}_{song.Id:X2}_Ch{song.ChNum}_{subNo:X}", "SndCall_");
            subNo++;
        }
    }

    public override int SizeInRom() => 0;

    public override void WriteToDisasm(MultiWriter sw)
    {
    }
}
