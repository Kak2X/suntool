using SunCommon;

namespace SunDis;

public abstract class OpcodeParser(Stream s)
{
    protected abstract SndOpcode GetOpcode(SndChHeader song, GbPtr p, int cmd);
    public GbOpcode Parse(SndFunc song)
    {
        var loc = GbPtrPool.Create(s.Position);
        var op = GetOpcode(song.Parent!.Parent!, loc, s.ReadByte());
        op.Parent = song;
        return new GbOpcode(loc, op);
    }

    public static OpcodeParser Create(Stream s, FormatOptions opt) => opt.Mode switch
    {
        DataMode.KOF95 => new OpcodeParser95(s),
        DataMode.KOF96 => new OpcodeParser96(s),
        DataMode.OP => new OpcodeParserOp(s),
        _ => throw new ArgumentOutOfRangeException(nameof(opt)),
    };

    public SndOpcode CmdCall() => new CmdCallEx
    {
        TargetPtr = s.ReadLocalPtr()
    };

    public SndOpcode CmdChanStop(PriorityGroup? priority = null, bool noFadeChg = false) => new CmdChanStop
    {
        Priority = priority,
        NoFadeChg = noFadeChg,
    };

    public SndOpcode CmdClrVibrato() => new CmdClrVibrato
    {
    };

    public SndOpcode CmdDutyCycle()
    {
        var x = s.ReadByte();
        return new CmdDutyCycle
        {
            Duty = (x >> 6) & 0b11,
            Length = x & 0b111111,
        };
    }

    public SndOpcode CmdPanning() => new CmdPanning
    {
        Pan = s.ReadByte()
    };

    public SndOpcode CmdEnvelope() => new CmdEnvelope
    {
        Arg = s.ReadByte()
    };

    public SndOpcode CmdErr(int cmd) => new CmdErr
    {
        Cmd = cmd
    };

    public SndOpcode CmdExtendNote() => new CmdExtendNote
    {
        Length = s.ReadByte()
    };

    public SndOpcode CmdFineTune() => new CmdFineTune
    {
        Offset = s.ReadByte()
    };

    public SndOpcode CmdFineTuneValue() => new CmdFineTuneValue
    {
        Offset = s.ReadByte()
    };

    public SndOpcode CmdLockEnv() => new CmdLockEnv
    {
    };

    public SndOpcode CmdLoopCnt() => new CmdLoopCntEx
    {
        TimerId = s.ReadByte(),
        TimerVal = s.ReadByte(),
        TargetPtr = s.ReadLocalPtr(),
    };

    public SndOpcode CmdLoop() => new CmdLoopEx
    {
        TargetPtr = s.ReadLocalPtr(),
    };

    public SndOpcode CmdNoisePoly(int cmd)
    {
        // Only use the length if the next byte is < 0x7F.
        // Done like with IMacroLength.
        int? length = s.ReadUint8();
        if (length > 0x7F)
        {
            length = null;
            s.Seek(-1, SeekOrigin.Current);
        }
        
        if (Consts.TbmNoiseNotes.Contains((byte)(cmd & 0xF7)))
        {
            return new CmdNoisePoly
            {
                Note = SciNote.CreateFromNoise(cmd),
                Length = length
            };
        }
        else
        {
            return new CmdNoisePolyCustom
            {
                RawValue = cmd,
                Length = length
            };
        }
    }

    public SndOpcode CmdNoisePolyPreset(int cmd) => new CmdNoisePolyPreset
    {
        PresetId = cmd - (int)SndCmdType.SNDNOTE_BASE,
    };

    public SndOpcode CmdNote(int cmd) => new CmdNote
    {
        Note = SciNote.Create(cmd - (int)SndCmdType.SNDNOTE_BASE),
    };

    public  SndOpcode CmdRet() => new CmdRet
    {
    };

    public SndOpcode CmdSlide() => new CmdSlide
    {
        Length = s.ReadByte(),
        Note = SciNote.Create(s.ReadByte()),
    };

    public SndOpcode CmdSpeed() => new CmdSpeed
    {
        Arg = s.ReadUint16(),
    };

    public SndOpcode CmdSweep() => new CmdSweep
    {
        Arg = s.ReadByte(),
    };

    public SndOpcode CmdUnlockEnv() => new CmdUnlockEnv
    {
    };

    public SndOpcode CmdVibrato96() => new CmdVibrato96
    {
    };

    public SndOpcode CmdVibratoOp() => new CmdVibratoOp
    {
        VibratoId = s.ReadByte() / 3,
    };

    public SndOpcode CmdWait(int cmd) => new CmdWait
    {
        Length = cmd,
    };

    public SndOpcode CmdWave() => new CmdWave
    {
        WaveId = s.ReadByte(),
    };

    public SndOpcode CmdWaveCutoff() => new CmdWaveCutoff
    {
        Length = s.ReadByte(),
    };

    public SndOpcode CmdWaveVol() => new CmdWaveVol
    {
        // Normalized, making it comparable to CndEnv
        Vol = (((s.ReadByte() >> 5) - 1 ^ 0xFF) & 3) << 6,
    };
}