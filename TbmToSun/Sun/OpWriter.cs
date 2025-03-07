﻿using SunCommon;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using static SunCommon.Sun.Consts;
using static TbmToSun.TbmModule;

namespace TbmToSun;

public static partial class OpWriter
{
    [GeneratedRegex(@"[^\w_]")]
    private static partial Regex LabelNormalizer();

    public static void Write(TbmModule module, MultiWriter output, string? baseTitle, bool sfx, string? vibratoPrefix)
    {
        // Import paths, to append to "song_includes.txt"
        var songPaths = new List<string>();

        // Initialize song_list.asm
        var scriptSongId = 1;
        output.ChangeFile("driver/data/song_list.asm",
            () => SndListBegin,
            (file) =>
            {
                // Autodetect the song ID from how many times "dsong" appears at the start of a line.
                scriptSongId = 0;
                foreach (var line in file.Split("\r\n"))
                    if (line.TrimStart().StartsWith("dsong"))
                        scriptSongId++;
                // Remove the .end: at the end of the file
                return file[..file.LastIndexOf(SndListEndMarker)];
            });

        var vibratoMap = new Dictionary<int, int?>();
        if (vibratoPrefix != null)
            foreach (var instr in module.Instruments)
            {
                var pos = instr.Name.IndexOf(vibratoPrefix);
                if (pos != -1)
                {
                    var hexDigit = instr.Name.Substring(pos + vibratoPrefix.Length, 2);
                    if (int.TryParse(hexDigit, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var vibId))
                        vibratoMap.Add(instr.Id, vibId);
                    else
                        Console.WriteLine($"Instrument '{instr.Name}' could not be mapped to a vibrato.");
                }
            }

        for (var sn = 0; sn < module.Songs.Length; sn++)
        {
            var srcSong = module.Songs[sn];

            var songName = baseTitle == null 
                ? LabelNormalizer().Replace(srcSong.Name, "")
                : (module.Songs.Length == 1 ? baseTitle : $"{baseTitle}_N{sn}");
            var title = (sfx ? "SFX_" : "BGM_") + songName;

            var songPath = $"driver/{(sfx ? "sfx/sfx_" : "bgm/bgm_")}{songName}.asm";
            songPaths.Add(songPath);
            output.ChangeFile(songPath);
            
            var chCount = 0;
            var bufCh = "";
            var bufData = new StringBuilder();

            var song = srcSong.ToPretty();
            Emit(song.Ch1);
            Emit(song.Ch2);
            Emit(song.Ch3);
            Emit(song.Ch4);

            output.Write(
$@"SndHeader_{title}:
	db ${chCount:X2} ; Number of channels
{bufCh}{bufData}");

            // Autodetect the init code
            string initCode;
            if (!sfx)
                initCode = SndInitNewBgm;
            else if (song.Ch1 != null)
                initCode = SndInitNewSfx1234;
            else if (song.Ch2 != null || song.Ch3 != null)
                initCode = SndInitNewSfx234;
            else
                initCode = SndInitNewSfx4;
            output.ChangeFile("driver/data/song_list.asm", append: true);
            output.WriteLine($"\tdsong SndHeader_{title}, {initCode}_\\1 ; ${(scriptSongId + 0x80):X02}");
            scriptSongId++;

            void Emit(PrettySong.PrettyChannel? chData)
            {
                if (chData == null)
                    return;
                chCount++;

                var chId = (int)chData.Channel + 1;
                var lbl = $"SndData_{title}_Ch{chId}";

                bufCh += 
$@".ch{chId}:
	db {(sfx ? "SIS_SFX|" : "")}SIS_ENABLED ; Initial playback status
	db SND_CH{chId}_PTR ; Sound channel ptr
	dw {lbl} ; Data ptr
	db 0 ; Initial fine tune
	db $81 ; Unused
";
                bufData.Append(chData.Channel switch
                {
                    ChannelType.Ch1 => (
$@"{lbl}:
	; envelope $98
	panning $11
	; duty_cycle 0
	; vibrato_on $01
"),
                    ChannelType.Ch2 => (
$@"{lbl}:
	; envelope $98
	panning $22
	; duty_cycle 0
	;vibrato_on $01
"),
                    ChannelType.Ch3 => (
$@"{lbl}:
	wave_vol $C0
	panning $44
	wave_id $01
	;wave_cutoff 0
"),
                    ChannelType.Ch4 => (
$@"{lbl}:
	panning $88
	; envelope $91
"),
                    _ => throw new ArgumentOutOfRangeException(nameof(chData)),
                });

                var usedPatterns = new HashSet<int>();
                foreach (int od in chData.TrackOrder)
                    usedPatterns.Add(od);

                // Determines which patterns are used only once (read: should be inlined)
                var defaultCallables = chData.TrackOrder.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count() > 1);
                var ticksPerRow = song.Speed.Rounded;
                var noiseLsfr = 0;

                var macroPat = new Dictionary<int, MacroSub>();
                foreach (var i in usedPatterns)
                {
                    if (!chData.Tracks.ContainsKey(i))
                    {
                        Console.WriteLine($"WARNING: Pattern Ch{((int)chData.Channel+1)}.{i} is missing, skipping.");
                        macroPat.Add(i, new MacroSub(i, chData.Channel, false));
                        continue;
                    }
                    var pattern = chData.Tracks[i];
                    var curMacro = ProcessRows(pattern.Rows);
                    macroPat.Add(i, curMacro);

                    MacroSub ProcessRows(PrettySong.PrettyRow[] rows)  
                    {
                        var curMacro = new MacroSub(i, chData.Channel, defaultCallables[pattern.TrackId]);
                        foreach (var row in rows)
                        {
                            if (row.Instrument.HasValue)
                                curMacro.MacroLength = module.Instruments.Length > row.Instrument ? module.Instruments[row.Instrument.Value].MacroLength : null;
                 
                            var noteLength = ticksPerRow;
                            var silenceLength = 0;

                            foreach (var x in row.Effects)
                            {
                                switch (x.EffectType)
                                {
                                    case EffectType.PatternHalt:
                                        // Halt immediately ends the channel on the spot
                                        curMacro.TerminatorEnd = true;
                                        return curMacro;
                                    case EffectType.SetTempo:
                                        // Usually noteLength = ticksPerRow
                                        var silenceDone = ticksPerRow - noteLength;
                                        ticksPerRow = new PrettySongSpeed(x.EffectParam).Rounded;
                                        noteLength = ticksPerRow - silenceDone - silenceLength;
                                        break;
                                    case EffectType.Sfx when chData.Channel == ChannelType.Ch3: // wave
                                        curMacro.WriteCommand($"wave_cutoff {x.EffectParam}");
                                        break;
                                    case EffectType.Sfx when chData.Channel == ChannelType.Ch4: // noise
                                        Console.WriteLine("noise_cutoff not implemented.");
                                        break;
                                    case EffectType.SetEnvelope when chData.Channel == ChannelType.Ch3:
                                        curMacro.WriteCommand($"wave_id ${(x.EffectParam + 1):X2}");
                                        break;
                                    case EffectType.SetEnvelope:
                                        curMacro.WriteCommand($"envelope ${x.EffectParam:X2}");
                                        break;
                                    case EffectType.SetTimbre when chData.Channel == ChannelType.Ch3:
                                        curMacro.WriteCommand($"wave_vol ${(x.EffectParam << 6):X2}");
                                        break;
                                    case EffectType.SetTimbre when chData.Channel == ChannelType.Ch4:
                                        noiseLsfr = x.EffectParam << 3;
                                        break;
                                    case EffectType.SetTimbre:
                                        curMacro.WriteCommand($"duty_cycle {x.EffectParam}");
                                        break;
                                    case EffectType.SetPanning:
                                        // 00 (mute), 01 (left), 10 (right), 11 (both)
                                        var l = x.EffectParam & 0b1;
                                        var r = x.EffectParam >> 1;
                                        curMacro.WriteCommand($"panning ${(l << ((int)chData.Channel + 4)) + (r << (int)chData.Channel):X2}");
                                        break;
                                    case EffectType.SetSweep:
                                        curMacro.WriteCommand($"sweep ${x.EffectParam:X2}");
                                        break;
                                    case EffectType.DelayedCut:
                                        // Row has note + silence
                                        noteLength = x.EffectParam;
                                        silenceLength = ticksPerRow - x.EffectParam;
                                        break;
                                    case EffectType.DelayedNote:
                                        // Row has silence + note
                                        curMacro.IncWaited(x.EffectParam);
                                        noteLength -= x.EffectParam;
                                        break;
                                    case EffectType.Lock:
                                        break;
                                    case EffectType.Arpeggio:
                                        break;
                                    case EffectType.PitchUp:
                                        break;
                                    case EffectType.PitchDown:
                                        break;
                                    case EffectType.AutoPortamento:
                                        break;
                                    case EffectType.Vibrato:
                                        break;
                                    case EffectType.VibratoDelay:
                                        break;
                                    case EffectType.Tuning:
                                        curMacro.WriteCommand($"fine_tune {x.EffectParam - 0x80}");
                                        break;
                                    case EffectType.NoteSlideUp:
                                        break;
                                    case EffectType.NoteSlideDown:
                                        break;
                                    case EffectType.SetGlobalVolume:
                                        break;
                                }
                            }

                            switch (row.Note)
                            {
                                case 85:
                                    curMacro.WriteSilence(noteLength);
                                    break;
                                case 0:
                                    curMacro.IncWaited(noteLength);
                                    break;
                                default:
                                    if (vibratoPrefix != null)
                                    {
                                        var curVib = vibratoMap.GetValueOrDefault(row.Instrument.GetValueOrDefault());
                                        if (curVib != curMacro.LastVibrato)
                                        {
                                            curMacro.WriteCommand(curVib.HasValue ? $"vibrato_on ${curVib:X2}" : "vibrato_off");
                                            curMacro.LastVibrato = curVib;
                                        }
                                    }

                                    string cmd;
                                    if (chData.Channel == ChannelType.Ch4)
                                    {
                                        var noteId = SciNote.NoiseNoteTable[row.Note - 1] + noiseLsfr;
                                        cmd = $"note4 {SciNote.ToNoteAsm(SciNote.CreateFromNoise(noteId))}";
                                    }
                                    else
                                    {
                                        cmd = $"note {SciNote.ToNoteAsm(SciNote.Create(row.Note))}";
                                        curMacro.QueueSilence();
                                    }
                                    curMacro.WriteCommand(cmd, noteLength, true);
                                    break;
                            }

                            if (silenceLength > 0)
                                curMacro.WriteSilence(silenceLength);

                            // Delayed effects allow the current note to play (with the necessary automatic delay)
                            if (row.DelayedEffect.HasValue)
                            {
                                var x = row.DelayedEffect.Value;
                                switch (x.EffectType)
                                {
                                    case EffectType.PatternGoto:
                                        curMacro.TerminatorGoto = x.EffectParam;
                                        return curMacro;
                                    case EffectType.PatternSkip:
                                        curMacro.TerminatorSkip = x.EffectParam;
                                        return curMacro;
                                }
                            }
                        }
                        return curMacro;
                    }
                }

                var mainDef = new StringBuilder();
                var subDef = new StringBuilder();
                mainDef.AppendLine($"\t{lbl}_Main:");

                var subPrintedOnce = new HashSet<int>();
                for (int i = 0; i < chData.TrackOrder.Length; i++)
                {
                    if (macroPat.TryGetValue(chData.TrackOrder[i], out var pat))
                    {
                        // Inline the pattern or not?
                        if (!pat.IsCallable)
                        {
                            // Inlined
                            mainDef.AppendLine($".P{i}: ; {lbl}_P{pat.Id}");

                            if (subPrintedOnce.Add(pat.Id))
                            {
                                if (pat.TerminatorEnd)
                                    pat.WriteCommand("chan_stop");
                                else if (pat.TerminatorGoto.HasValue)
                                {
                                    pat.WriteCommand($"snd_loop {lbl}_Main.P{pat.TerminatorGoto.Value}");
                                    pat.Looped = true;
                                }
                                else if (pat.TerminatorSkip.GetValueOrDefault() != 0) // PatternSkip 0 just ends early
                                    Console.WriteLine("PatternSkip > 0 not implemented");
                                else
                                    pat.WaitFlush();
                            }
                            mainDef.Append(pat.Body);
                        }
                        else
                        {
                            mainDef.AppendLine($".P{i}: snd_call {lbl}_P{pat.Id}");
                            if (subPrintedOnce.Add(pat.Id))
                            {
                                subDef.AppendLine($"{lbl}_P{pat.Id}:");

                                if (pat.TerminatorEnd)
                                    pat.WriteCommand("chan_stop");
                                else if (pat.TerminatorGoto.HasValue)
                                {
                                    if (pat.TerminatorGoto.Value == chData.TrackOrder[0])
                                        pat.WriteCommand("snd_ret");
                                    else
                                        Debug.Fail("PatternGoto not pointing to top not implemented.");
                                }
                                else if (pat.TerminatorSkip.GetValueOrDefault() != 0) // PatternSkip 0 is ret
                                    Console.WriteLine("PatternSkip > 0 not implemented");
                                else
                                    pat.WriteCommand("snd_ret");

                                subDef.Append(pat.Body);
                            }
                        }
                    }
                }

                // Check if we need an explicit loop command after all patterns
                var lastPat = macroPat.GetValueOrDefault(chData.TrackOrder.Last());
                if (lastPat == null || !lastPat.Looped || lastPat.TerminatorEnd)
                    mainDef.AppendLine($"\tsnd_loop {lbl}_Main");

                bufData.Append(mainDef);
                bufData.Append(subDef);
            }
        }

        output.ChangeFile("driver/data/waves.asm");

        output.WriteLine("Sound_WaveSetPtrTable_\\1:");
        for (var i = 0; i < module.Waves.Length; i++)
            output.WriteIndent($"dw Sound_WaveSet{i}_\\1");
        for (var i = 0; i < module.Waves.Length; i++)
            output.WriteLine($"Sound_WaveSet{i}_\\1: db {module.Waves[i].Data.FormatByte()} ; ${module.Waves[i].Id:X02} ; {module.Waves[i].Name} \r\n");

        output.ChangeFile("driver/data/song_list.asm", append: true);
        output.WriteLine(SndListEndMarker);

        output.ChangeFile("driver/song_includes.txt", append: true);
        foreach (var x in songPaths)
            output.WriteLine($"INCLUDE \"{x.Replace('\\', '/')}\"");
    }

    private class MacroSub
    {
        /// <summary>Pattern ID</summary>
        public int Id;
        /// <summary>Channel ID</summary>
        public ChannelType Channel;
        /// <summary>Pattern rows converted to OP macros.</summary>
        public StringBuilder Body;
        /// <summary>If the pattern can be called through snd_call. If not, it has to be inlined.</summary>
        public bool IsCallable;
        /// <summary>If the pattern ends with Bxx.</summary>
        public int? TerminatorGoto;
        /// <summary>If the pattern ends with Dxx.</summary>
        public int? TerminatorSkip;
        /// <summary>If the sound channel ends with this.</summary>
        public bool TerminatorEnd;
        /// <summary>Last vibrato used.</summary>
        public int? LastVibrato;

        public MacroSub(int id, ChannelType ch, bool isCallable)
        {
            Id = id;
            Channel = ch;
            Body = new();
            IsCallable = isCallable;
            LastVibrato = -1;
        }

        //--
        // Working

        internal int? MacroLength;
        private bool queueSilence;
        private int waited;
        private int lastLength;
        private bool lastIsNote;
        internal bool Looped;


        public void QueueSilence()
        {
            // Quirk in the driver causes ch3 to be continuous.
            // Require explicit silence if one is set.
            queueSilence = MacroLength.HasValue && Channel == ChannelType.Ch3;
        }

        public void IncWaited(int amount)
        {
            if (queueSilence && waited + amount > MacroLength)
            {
                Debug.Assert(MacroLength.HasValue, "MacroLength should be set.");
                // When we go over the limit, amount will only be what's past MacroLength
                amount = waited + amount - MacroLength.Value;
                // While waited gets capped
                waited = MacroLength.Value;
                // And the final waited will be amount
                WriteSilence();
                queueSilence = false;
            }
            waited += amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WaitFlush()
        {
            Wait();
            waited = 0;
        }

        private void Wait()
        {
            // If we're waiting at the start of a subroutine, extend the previous note.
            // The noise channel also doesn't support standalone waits.
            if (waited > 0 && Body.Length == 0 || (Channel == ChannelType.Ch4 && !lastIsNote))
            {
                for (var i = waited; i > 0; i -= 0xFF)
                {
                    lastLength = Math.Min(i, 0xFF);
                    WriteRawCommand($"continue {lastLength}");
                }
            }
            // Same note length used until you change it.
            // Always set a length on the noise channel though, it malfunctions otherwise.
            else if (lastLength != waited || Channel == ChannelType.Ch4 || !lastIsNote)
            {
                var cmd = "wait";
                for (var i = waited; i > 0; i -= 0x7F, cmd = "continue")
                {
                    lastLength = Math.Min(i, 0x7F);
                    WriteRawCommand($"{cmd} {lastLength}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSilence(int length = 0)
        {
            if (Channel == ChannelType.Ch4)
                WriteCommand("envelope $00", length);
            else
                WriteCommand("silence", length, true);
        }

        public void WriteCommand(string command, int length = 0, bool isNote = false)
        {
            Wait();
            WriteRawCommand(command);
            waited = length;
            lastIsNote = isNote;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRawCommand(string command)
        {
            Body.AppendLine($"\t{command}");
        }
    }
}
