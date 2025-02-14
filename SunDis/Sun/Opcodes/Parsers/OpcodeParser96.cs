namespace SunDis;

public class OpcodeParser96 : IOpcodeParser
{
    public SndOpcode Parse(Stream s, SongInfo song)
    {
        var p = GbPtrPool.Create(s.Position);
        var cmd = s.ReadByte();

        if (cmd >= (int)SndCmdType.SNDCMD_BASE)
        {
            cmd -= (int)SndCmdType.SNDCMD_BASE;
            return cmd switch
            {
                0x03 => new CmdChanStop(p),
                0x04 when song.ChPtr == SndChPtrNum.SND_CH3_PTR => new CmdWaveVol(p, s),
                0x04 => new CmdEnvelope(p, s),
                0x05 => new CmdLoop(p, s, song),
                0x06 => new CmdFineTune(p, s),
                0x07 => new CmdLoopCnt(p, s, song),
                0x08 => new CmdSweep(p, s),
                0x09 => new CmdEnaCh(p, s),
                0x0C => new CmdCall(p, s),
                0x0D => new CmdRet(p),
                0x0E when song.ChPtr < SndChPtrNum.SND_CH3_PTR => new CmdDutyCycle(p, s),
                0x0E => throw new Exception("Attempted to use old CmdCutoff"), //new CmdCutoff(p, s),
                0x0F => new CmdLockEnv(p),
                0x10 => new CmdUnlockEnv(p),
                0x11 => new CmdVibrato96(p),
                0x12 => new CmdClrVibrato(p),
                0x13 => new CmdWave(p, s),
                0x14 => new CmdChanStop(p, PriorityGroup.SNP_SFXMULTI),
                0x15 => new CmdWaveCutoff(p, s),
                0x16 => new CmdChanStop(p, PriorityGroup.SNP_SFX4),
                0x1A => new CmdExtendNote(p, s),
                _ => new CmdErr(p, cmd),
            };
        }
        if (song.ChPtr == SndChPtrNum.SND_CH4_PTR)
            return new CmdNoisePoly(p, cmd, s);
        else if (cmd < (int)SndCmdType.SNDNOTE_BASE)
            return new CmdWait(p, cmd);
        else
            return new CmdNote(p, cmd);
    }
}
