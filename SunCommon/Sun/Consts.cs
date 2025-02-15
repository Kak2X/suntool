namespace SunCommon.Sun
{
    public static class Consts
    {
        public const string SndListBegin = @"; =============== Sound_SndListTable ===============
; Table of sound assignments, ordered by ID.
Sound_SndListTable_\1:
IF \2
Sound_SndListTable_Main:
ENDC
	dsong Sound_SndListTable_Main, Sound_StartNothing_\1 ; $80
";
        public const string SndInitPause = "Sound_PauseAll";
        public const string SndInitUnpause = "Sound_UnpauseAll";
        public const string SndInitNewBgm = "Sound_StartNewBGM";
        public const string SndInitNewSfx1234 = "Sound_StartNewSFX1234";
        public const string SndInitNewSfx234 = "Sound_StartNewSFX234";
        public const string SndInitNewSfx4 = "Sound_StartNewSFX4";
        public const string SndInitDummy = "Sound_SndListTable_Main";
        public const string SndListEndMarker = ".end:";
    }
}
