using SunCommon;
using System.Diagnostics;

namespace TbmToSun;

public class TbmModule
{
    public readonly int Version;
    public readonly SongBlock[] Songs;
    public readonly InstBlock[] Instruments;
    public readonly WaveBlock[] Waves;

    /// <summary>
    ///     Length of wave channel notes.
    /// </summary>
    public readonly int? WaveMacroLength;

    public TbmModule(Stream s)
    {
        s.Seek(24, SeekOrigin.Begin);
        Version = s.ReadUint8();
        s.Seek(124, SeekOrigin.Begin);

        var instrumentCount = s.ReadUint8();
        var songCount = s.ReadBiasedUint8();
        var waveCount = s.ReadUint8();
        var tickRate = GetTickRate(s);

        s.Seek(160, SeekOrigin.Begin);

        // Read sections

        // Skip COMM
        s.Seek(4, SeekOrigin.Current);
        s.Seek(s.ReadUint32(), SeekOrigin.Current);

        // SONG
        Songs = new SongBlock[songCount];
        for (int i = 0; i < songCount; i++)
            Songs[i] = new SongBlock(this, s);

        // INST
        Instruments = new InstBlock[instrumentCount];
        for (int i = 0; i < instrumentCount; i++)
        {
            var inst = new InstBlock(this, s);
            Instruments[i] = inst;
            if (inst.Channel == ChannelType.Ch3)
                WaveMacroLength = inst.GetTimbreLength();
        }

        // WAVE
        Waves = new WaveBlock[waveCount];
        for (int i = 0; i < waveCount; i++)
            Waves[i] = new WaveBlock(s);
    }



    private float GetTickRate(Stream s)
    {
        if (Version >= 2)
        {
            switch (s.ReadUint8())
            {
                case 0:
                    s.Seek(4, SeekOrigin.Current);
                    return 59.7F;
                case 1:
                    s.Seek(4, SeekOrigin.Current);
                    return 61.1F;
                case 2:
                    return s.ReadFloat32();
                default:
                    throw new Exception("Bad speed value.");
            }
        } 
        else
        {
            switch (s.ReadUint8())
            {
                case 0:
                    s.Seek(2, SeekOrigin.Current);
                    return 59.7F;
                case 1:
                    s.Seek(2, SeekOrigin.Current);
                    return 61.1F;
                case 2:
                    return (float)s.ReadUint16();
                default:
                    throw new Exception("Bad speed value.");
            }
        }

    }

    public readonly struct PrettySongSpeed
    {
        public readonly decimal Decimal;
        public readonly int Rounded;

        public PrettySongSpeed(int tbmRawSpeed)
        {
            Decimal = tbmRawSpeed / (decimal)0x10;
            Rounded = (int)Math.Round(Decimal);
        }
    }

    /// <summary>
    ///     More convenient version of a <see cref="SongBlock"/>.
    /// </summary>
    public class PrettySong
    {
        public readonly string Name;
        public readonly PrettySongSpeed Speed;
        public readonly PrettyChannel? Ch1;
        public readonly PrettyChannel? Ch2;
        public readonly PrettyChannel? Ch3;
        public readonly PrettyChannel? Ch4;

        public PrettySong(SongBlock src)
        {
            Name = src.Name;
            Speed = new PrettySongSpeed(src.Speed);

            var groupedTracks = src.Tracks
                .GroupBy(x => x.Channel)
                .ToDictionary(x => x.Key, x => x);

            SetChannel(ref Ch1, ChannelType.Ch1, x => x.Ch1);
            SetChannel(ref Ch2, ChannelType.Ch2, x => x.Ch2);
            SetChannel(ref Ch3, ChannelType.Ch3, x => x.Ch3);
            SetChannel(ref Ch4, ChannelType.Ch4, x => x.Ch4);

            void SetChannel(ref PrettyChannel? chField, ChannelType ch, Func< SongBlock.SongOrderFormat, int> orderFn)
            {
                if (groupedTracks.TryGetValue(ch, out var tracks))
                {
                    chField = new PrettyChannel(ch, tracks, src.SongOrder.Select(orderFn), src.RowsPerTrack);
                }
            }
        }

        public class PrettyChannel
        {
            public readonly ChannelType Channel;
            public readonly int[] TrackOrder;
            public readonly Dictionary<int, PrettyTrack> Tracks;

            public PrettyChannel(ChannelType ch, IEnumerable<SongBlock.TrackFormat> tracks, IEnumerable<int> order, int rowsPerTrack)
            {
                Channel = ch;
                TrackOrder = order.ToArray();
                // Track IDs are NOT guaranteed to be sequential.
                Tracks = tracks.Select(x => new PrettyTrack(x, rowsPerTrack)).ToDictionary(x => x.TrackId, x => x);
            }
        }

        public class PrettyTrack
        {
            public readonly PrettyRow[] Rows;
            public readonly int TrackId;

            public PrettyTrack(SongBlock.TrackFormat src, int rowsPerTrack)
            {
                TrackId = src.TrackId;
                // Inflate the empty rows
                Rows = Enumerable.Repeat(new PrettyRow(), rowsPerTrack).ToArray();
                foreach (var row in src.Rows)
                    Rows[row.RowNumber] = new PrettyRow(row);
            }
        }

        public class PrettyRow
        {
            public readonly int Note;
            public readonly int Instrument;
            public readonly List<SongBlock.TrackFormat.RowFormat.Effect> Effects = new(3);
            public readonly SongBlock.TrackFormat.RowFormat.Effect? DelayedEffect;

            public PrettyRow()
            {
            }
            public PrettyRow(SongBlock.TrackFormat.RowFormat src)
            {
                Note = src.Note;
                Instrument = src.Instrument;

                // Split between delayed and undelayed effects.
                // Delayed effects
                foreach (var effect in src.Effects)
                {
                    if (effect.EffectType == EffectType.PatternGoto || effect.EffectType == EffectType.PatternSkip)
                        DelayedEffect = effect;
                    else if (effect.EffectType != EffectType.NoEffect)
                        Effects.Add(effect);
                }
            }

            public bool IsEmptyRow() => Note == 0 && Effects.Count == 0 && DelayedEffect == null;
        }
    }

    public class SongBlock
    {
        public readonly string Name;

        public readonly int RowsPerBeat;
        public readonly int RowsPerMeasure;
        public readonly int Speed;
        public readonly int RowsPerTrack;
        public readonly int NumberOfEffects;
        public readonly float CustomFramerateOverride;
        public readonly SongOrderFormat[] SongOrder;
        public readonly TrackFormat[] Tracks;

        public SongBlock(TbmModule parent, Stream s)
        {
            var chk = s.ReadString(4);
            Debug.Assert(chk == "SONG");
            s.Seek(4, SeekOrigin.Current);

            Name = s.ReadLString();
            RowsPerBeat = s.ReadBiasedUint8();
            RowsPerMeasure = s.ReadBiasedUint8();
            Speed = s.ReadUint8();
            var PatternCount = s.ReadBiasedUint8();
            RowsPerTrack = s.ReadBiasedUint8();
            var NumberOfTracks = s.ReadUint16();
            NumberOfEffects = s.ReadUint8();
            if (parent.Version >= 2)
                CustomFramerateOverride = parent.GetTickRate(s);

            SongOrder = new SongOrderFormat[PatternCount];
            for (var i = 0; i < PatternCount; i++)
                SongOrder[i] = new SongOrderFormat(s);

            Tracks = new TrackFormat[NumberOfTracks];
            for (var i = 0; i < NumberOfTracks; i++)
                Tracks[i] = new TrackFormat(s);
        }

        public PrettySong ToPretty() => new(this);

        public class SongOrderFormat
        {
            public readonly int Ch1;
            public readonly int Ch2;
            public readonly int Ch3;
            public readonly int Ch4;

            public SongOrderFormat(Stream s)
            {
                Ch1 = s.ReadByte();
                Ch2 = s.ReadByte();
                Ch3 = s.ReadByte();
                Ch4 = s.ReadByte();
            }
        }

        public class TrackFormat
        {
            public readonly ChannelType Channel;
            public readonly int TrackId;
            public readonly RowFormat[] Rows;

            public TrackFormat(Stream s)
            {
                Channel = (ChannelType)s.ReadUint8();
                TrackId = s.ReadUint8();
                var rowCount = s.ReadBiasedUint8(); // data rows (no blanks)
                Rows = new RowFormat[rowCount];
                for (var i = 0; i < rowCount; i++)
                    Rows[i] = new RowFormat(s);
            }


            public class RowFormat
            {
                public readonly int RowNumber;
                //--
                public readonly int Note;
                public readonly int Instrument;
                public readonly Effect[] Effects = new Effect[3];

                public RowFormat()
                {
                }
                public RowFormat(Stream s)
                {
                    RowNumber = s.ReadUint8();
                    Note = s.ReadUint8();
                    Instrument = s.ReadUint8();
                    for (var i = 0; i < 3; i++)
                        Effects[i] = new Effect(s);
                }

                public bool IsEmptyRow()
                {
                    return Note == 0
                        && Effects[0].EffectType == 0 && Effects[0].EffectParam == 0
                        && Effects[1].EffectType == 0 && Effects[1].EffectParam == 0
                        && Effects[2].EffectType == 0 && Effects[2].EffectParam == 0
                        ;
                }

                public struct Effect
                {
                    public readonly EffectType EffectType;
                    public readonly int EffectParam;

                    public Effect(Stream s)
                    {
                        EffectType = (EffectType)s.ReadUint8();
                        EffectParam = s.ReadUint8();
                    }
                }
            }
        }
    }

    public enum EffectType
    {
        NoEffect,
        PatternGoto,
        PatternHalt,
        PatternSkip,
        SetTempo,
        Sfx,
        SetEnvelope,
        SetTimbre,
        SetPanning,
        SetSweep,
        DelayedCut,
        DelayedNote,
        Lock,
        Arpeggio,
        PitchUp,
        PitchDown,
        AutoPortamento,
        Vibrato,
        VibratoDelay,
        Tuning,
        NoteSlideUp,
        NoteSlideDown,
        SetGlobalVolume,
    }

    public class InstBlock
    {
        public readonly int Id;
        public readonly string Name;
        public readonly ChannelType Channel;
        public bool? EnvelopeEnabled; // < V2
        public int? Envelope; // < V2
        public readonly SequenceFormat SeqArp;
        public readonly SequenceFormat SeqPanning;
        public readonly SequenceFormat SeqPitch;
        public readonly SequenceFormat SeqTimbre;
        public readonly SequenceFormat? SeqEnvelope;

        public InstBlock(TbmModule parent, Stream s)
        {
            var chk = s.ReadString(4);
            Debug.Assert(chk == "INST");
            s.Seek(4, SeekOrigin.Current);

            Id = s.ReadUint8();
            Name = s.ReadLString();
            Channel = (ChannelType)s.ReadUint8();
            if (parent.Version < 2)
            {
                EnvelopeEnabled = s.ReadBool();
                Envelope = s.ReadUint8();
            }
            SeqArp = new SequenceFormat(s);
            SeqPanning = new SequenceFormat(s);
            SeqPitch = new SequenceFormat(s);
            SeqTimbre = new SequenceFormat(s);
            if (parent.Version >= 2)
                SeqEnvelope = new SequenceFormat(s);
        }

        public class SequenceFormat
        {
            public readonly bool LoopEnabled;
            public readonly int LoopIndex;
            public readonly byte[] Data;

            public SequenceFormat(Stream s)
            {
                var length = s.ReadUint16();
                LoopEnabled = s.ReadBool();
                LoopIndex = s.ReadUint8();
                Data = new byte[length];
                s.Read(Data, 0, length);
            }
        }

        /// <summary>
        ///     Detects the length of the Timbre data.
        /// </summary>
        /// <remarks>
        ///     This is useful for getting the length of Wave notes, since TB doesn't implement wave note length directly.
        ///     The reason behind this is that, if set, the Timbre data regulates the note's volume across frames (1 entry/frame),
        ///     and the last one *repeats indefinitely*.
        ///     If the last entry is a 0, the note has a fixed length. Otherwise, it never goes off.
        /// </remarks>
        /// <returns></returns>
        public int? GetTimbreLength()
        {
            if (SeqTimbre.Data.Length > 0 && SeqTimbre.Data.Last() == 0) // The last entry loops, so if it is 0, the sound has a fixed end.
                for (var l = SeqTimbre.Data.Length - 1; l > 0; l--) // From the last entry to the first...
                    if (SeqTimbre.Data[l - 1] != 0) // Is the previous != 0?
                        return l; // If so, it ends on the current location
            return null;
        }
    }

    public class WaveBlock
    {
        public readonly int Id;
        public readonly string Name;
        public readonly byte[] Data;

        public WaveBlock(Stream s)
        {
            var chk = s.ReadString(4);
            Debug.Assert(chk == "WAVE");
            s.Seek(4, SeekOrigin.Current);

            Id = s.ReadUint8();
            Name = s.ReadLString();
            Data = new byte[16];
            s.Read(Data, 0, 16);
        }
    }
}
