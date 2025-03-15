namespace SunCommon;

public class PointerTable : IRomData
{
    public List<SndHeader> Songs { get; } = [];
    public DataMode Mode { get; set; } = DataMode.OP;

    public int SizeInRom()
    {
        return Mode switch
        {
            DataMode.OP => (Songs.Count + 1) * 3,
            DataMode.OPEx => (Songs.Count + 1) * 5,
            _ => (Songs.Count + 1) * 2,
        };
    }
    public string? GetLabel() => null;
    public void WriteToDisasm(IMultiWriter sw)
    {
        if (Mode == DataMode.OPEx)
        {
            sw.Write(Consts.SndListBegin);
            foreach (var song in Songs)
            {
                var initCode = song.Kind switch
                {
                    SongKind.Pause => Consts.SndInitPause,
                    SongKind.Unpause => Consts.SndInitUnpause,
                    SongKind.BGM => Consts.SndInitNewBgm,
                    _ => song.Channels.Min(x => x.SoundChannelPtr) switch
                    {
                        SndChPtrNum.SND_CH1_PTR => Consts.SndInitNewSfx1234,
                        SndChPtrNum.SND_CH2_PTR or SndChPtrNum.SND_CH3_PTR => Consts.SndInitNewSfx234,
                        SndChPtrNum.SND_CH4_PTR => Consts.SndInitNewSfx4,
                        _ => Consts.SndInitDummy,
                    },
                };
                sw.WriteLine($"\tdsong {song.GetLabel()}, {initCode}_\\1 ; {(song.Id + 0x80).AsHexByte()}");
            }
            sw.WriteLine($".end:");
        } 
        else
        {
            sw.WriteLine("Sound_SndHeaderPtrTable:");
            foreach (var x in Songs)
                sw.WriteIndent($"dw {x.GetLabel()} ; {(x.Id + 0x80).AsHexByte()}");
            if (Mode == DataMode.OP)
            {
                sw.WriteLine("Sound_SndBankPtrTable:");
                foreach (var x in Songs)
                    sw.WriteIndent($"db BANK({x.GetLabel()}) ; {(x.Id + 0x80).AsHexByte()}");
            }
        }
    }
}
