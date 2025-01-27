namespace SunDis;

[Flags]
public enum SndInfoStatus
{
    SIS_PAUSE = 1 << 0,
    SIS_LOCKNRx2 = 1 << 1,
    SIS_USEDBYSFX = 1 << 2,
    SIS_SFX = 1 << 3,
    SIS_SLIDE = 1 << 5,
    SIS_VIBRATO = 1 << 6,
    SIS_ENABLED = 1 << 7,
}
