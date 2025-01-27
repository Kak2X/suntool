namespace SunDis;

public class CmdLoopCnt : SndOpcode, IHasPointer
{
    public readonly int TimerId;
    public readonly int TimerVal;
    public GbPtr Target { get; set; }

    public CmdLoopCnt(GbPtr p, Stream s, SongInfo song) : base(p)
    {
        TimerId = s.ReadByte();
        TimerVal = s.ReadByte();
        Target = s.ReadLocalPtr();
        if (Target.SetLabelIfNew($".loop{song.LoopCount}"))
            song.LoopCount++;
    }

    public override int SizeInRom() => 5;

    public override void WriteToDisasm(MultiWriter sw)
    {
        sw.WriteCommand("snd_loop", Target.ToLabel(), $"${TimerId:X2}", $"{TimerVal}");
    }
}
