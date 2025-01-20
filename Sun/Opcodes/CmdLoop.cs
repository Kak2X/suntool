namespace SunnyDay;

public class CmdLoop : SndOpcode, IHasPointer
{
    public GbPtr Target { get; set; }

    public CmdLoop(GbPtr p, Stream s, SongInfo song) : base(p)
    {
        Terminates = true;
        Target = s.ReadLocalPtr();
        if (Target.SetLabelIfNew($".loop{song.LoopCount}"))
            song.LoopCount++;
    }

    public override int SizeInRom() => 3;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_loop", Target.ToLabel());
    }
}
