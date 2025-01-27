using SunCommon;

namespace SunDis;

public class CmdCall : SndOpcode, IHasPointer
{
    public GbPtr Target {  get; set; }

    public CmdCall(GbPtr p, Stream s) : base(p)
    {
        Target = s.ReadLocalPtr();
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_call", Target.ToLabel());
    }
}
