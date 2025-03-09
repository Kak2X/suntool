using SunCommon;

namespace SunDis;

public class OpcodeParser96(Stream s) : OpcodeParser(s)
{
    protected override SndOpcode GetOpcode(SndChHeader song, GbPtr p, int cmd)
    {
        if (cmd >= (int)SndCmdType.SNDCMD_BASE)
        {
            cmd -= (int)SndCmdType.SNDCMD_BASE;
            return cmd switch
            {
                0x03 => CmdChanStop(),
                0x04 when song.SoundChannelPtr == SndChPtrNum.SND_CH3_PTR => CmdWaveVol(),
                0x04 => CmdEnvelope(),
                0x05 => CmdLoop(),
                0x06 => CmdFineTune(),
                0x07 => CmdLoopCnt(),
                0x08 => CmdSweep(),
                0x09 => CmdEnaCh(),
                0x0C => CmdCall(),
                0x0D => CmdRet(),
                0x0E when song.SoundChannelPtr < SndChPtrNum.SND_CH3_PTR => CmdDutyCycle(),
                0x0E => throw new Exception("Attempted to use old CmdCutoff"), //new CmdCutoff(),
                0x0F => CmdLockEnv(),
                0x10 => CmdUnlockEnv(),
                0x11 => CmdVibrato96(),
                0x12 => CmdClrVibrato(),
                0x13 => CmdWave(),
                0x14 => CmdChanStop(PriorityGroup.SNP_SFXMULTI),
                0x15 => CmdWaveCutoff(),
                0x16 => CmdChanStop(PriorityGroup.SNP_SFX4),
                0x1A => CmdExtendNote(),
                _ => CmdErr(cmd),
            };
        }
        if (song.SoundChannelPtr == SndChPtrNum.SND_CH4_PTR)
            return CmdNoisePoly(cmd);
        else if (cmd < (int)SndCmdType.SNDNOTE_BASE)
            return CmdWait(cmd);
        else
            return CmdNote(cmd);
    }
}
