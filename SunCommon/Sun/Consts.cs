namespace SunCommon
{
    public static class Consts
    {
        public static readonly byte[] TbmNoiseNotes =
        [
            0xD7,0xD6,0xD5,0xD4,0xC7,0xC6,0xC5,0xC4,0xB7,0xB6,0xB5,0xB4,
            0xA7,0xA6,0xA5,0xA4,0x97,0x96,0x95,0x94,0x87,0x86,0x85,0x84,
            0x77,0x76,0x75,0x74,0x67,0x66,0x65,0x64,0x57,0x56,0x55,0x54,
            0x47,0x46,0x45,0x44,0x37,0x36,0x35,0x34,0x27,0x26,0x25,0x24,
            0x17,0x16,0x15,0x14,0x07,0x06,0x05,0x04,0x03,0x02,0x01,0x00,
        ];

        public static readonly byte[][] NotePresets95 =
        [
            [0x00,0x00],
            [0x51,0x36],
            [0x52,0x24],
            [0x31,0x21],
            [0x53,0x11],
            [0x53,0x11],
            [0x53,0x11],
            [0x52,0x36],
            [0x52,0x36],
            [0x52,0x36],
        ];

        // First usable bank for dumping songs.
        public const int DefaultStartingBank = 1;

        private const int BaseDriverSize = 0x49F7 - 0x4000;

        // Default free space in a bank. (for TbmToSun, ignores vibrato & wave since they get written to)
        public const int FreeSpaceBase = 0x4000 - BaseDriverSize;

        // Default free space in a bank. (for SunDis, accounts for vibrato & wave)
        public const int FreeSpaceSongOnly = FreeSpaceBase - 0x026C;

        public const string SndMainBegin = @"mSOUNDBANK 03, 1 ; Main bank, as the last one for GBS compat (TODO: autodivide)
;   mSOUNDBANK 02
;   mSOUNDBANK 01
";

        public const string VibratoTblBegin = @"; =============== Sound_VibratoSetTable ===============
; Sets of vibrato data, usable by all channels.
Sound_VibratoSetTable_\1:";

        public const string WaveTblBegin = @"; =============== Sound_WaveSetPtrTable ===============
; Sets of Wave data for channel 3, copied directly to the rWave registers.
Sound_WaveSetPtrTable_\1:";

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
        public const string SndInitPriorityHi = "Hi";
        public const string SndInitPriorityLow = "Lo";
        public const string SndInitDummy = "Sound_SndListTable_Main";
        public const string SndListEndMarker = ".end:";
    }
}
