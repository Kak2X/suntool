namespace SunDis;

public enum SndCmdType : int
{
    SNDCMD_FADEIN = 0x10,
    SNDCMD_FADEOUT = 0x20,
    SNDCMD_CH1VOL = 0x30,
    SNDCMD_CH2VOL = 0x40,
    SNDCMD_CH3VOL = 0x50,
    SNDCMD_CH4VOL = 0x60,
    SNDCMD_BASE = 0xE0,
    SNDNOTE_BASE = 0x80,
}
