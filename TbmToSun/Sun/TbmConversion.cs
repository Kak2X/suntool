using SunCommon;
using SunCommon.Util;
using System.Diagnostics;
using static TbmToSun.TbmModule;

namespace TbmToSun;

public class TbmConversion
{
    public record TbmConvResult(PointerTable Songs, VibratoTable Vibratos, WaveTable Waves);
    private class SndFuncWrap
    {
        public required SndFunc Func;
        public int? TerminatorGoto;
        public int? TerminatorSkip;
        public bool StopCh;
    }
    public PointerTable PtrTbl = new();
    public VibratoTable Vibratos = new();
    public WaveTable Waves = new();
    public TbmConversion(InstructionSheet sheet)
    {
        PtrTbl.Mode = DataMode.OPEx;
        foreach (var song in sheet.Rows)
        {
            Console.WriteLine($"-> {song.Path}");
            Convert(song);
        }
    }



    private void Convert(InstructionSong sheetSong)
    {
        // Build the mappings to the module's Waves/Vibratos IDs to our IDs
        var waveMap = new Dictionary<int, int>();
        foreach (var x in sheetSong.Module.Waves)
            waveMap[x.Id] = Waves.Push(x.Data, x.Name);

        var vibratoMap = new Dictionary<int, int>();
        foreach (var x in sheetSong.Module.Instruments)
        {
            if (x.SeqPitch.Data.Length > 0)
            {
                // TBM vibratos use relative values.
                // Our vibratos use absolute values.
                var newVibrato = new byte[x.SeqPitch.Data.Length];
                for (byte i = 0, next = 0; i < newVibrato.Length; i++)
                {
                    unchecked { next += x.SeqPitch.Data[i]; }
                    newVibrato[i] = next;
                }
                vibratoMap[x.Id] = Vibratos.Push(newVibrato, x.SeqPitch.LoopIndex);
            }
        }

        var macroLenMap = sheetSong.Module.Instruments.ToDictionary(x => x.Id, x => x.MacroLength);

        foreach (var srcSong in sheetSong.Module.Songs)
        {
            Console.WriteLine($"---> {srcSong.Name}");

            var song = srcSong.ToPretty();
            var id = PtrTbl.Songs.Count + 1;

            // Autodetect an appropriate song name
            var songName = song.Name != null && song.Name != "New song" 
                ? song.Name 
                : sheetSong.Title;
            var res = new SndHeader
            {
                Id = id,
                Name = songName != null
                     ? $"{id:X2}_" + LabelNormalizer.Apply(songName)
                     : $"{id:X2}",
                Kind = sheetSong.Kind,
                Priority = sheetSong.Priority,
                ChannelCount = 0,
            };

            DoChannel(song.Ch1, sheetSong, waveMap, vibratoMap, macroLenMap, song, res);
            DoChannel(song.Ch2, sheetSong, waveMap, vibratoMap, macroLenMap, song, res);
            DoChannel(song.Ch3, sheetSong, waveMap, vibratoMap, macroLenMap, song, res);
            DoChannel(song.Ch4, sheetSong, waveMap, vibratoMap, macroLenMap, song, res);

            // Detect any gaps between sound channels, since the sound driver doesn't allow them
            for (var i = 0; i < res.Channels.Count - 1; i++)
            {
                var expectedNext = res.Channels[i].SoundChannelPtr.Next();
                var target = res.Channels[i + 1].SoundChannelPtr.Normalize();
                for (var j = expectedNext.Normalize(); j < target; j++, i++, expectedNext = expectedNext.Next())
                {
                    res.Channels.Insert(i + 1, new SndChHeader
                    {
                        SoundChannelPtr = expectedNext,
                        Parent = res,
                        Data = null,
                    });
                    res.ChannelCount++;
                }
            }

            PtrTbl.Songs.Add(res);
        }
    }

    private static void DoChannel(PrettySong.PrettyChannel? chData, InstructionSong sheetSong, Dictionary<int, int> waveMap, Dictionary<int, int> vibratoMap, Dictionary<int, int?> macroLenMap, PrettySong song, SndHeader res)
    {
        if (chData == null) return;
        res.ChannelCount++;

        var resCh = new SndChHeader
        {
            Parent = res,
            InitialStatus = SndInfoStatus.SIS_ENABLED | (res.Kind == SongKind.SFX ? SndInfoStatus.SIS_SFX : 0),
            Unused = 0x56, // :^)
        };
        Debug.Assert(resCh.Data != null);
        var initialWave = (CmdWave?)null;

        // Define initial commands at the start of a song
        switch (chData.Channel)
        {
            case ChannelType.Ch1:
                resCh.SoundChannelPtr = SndChPtrNum.SND_CH1_PTR;
                resCh.Data.Main.AddOpcode(new CmdPanning { Pan = 0x11 });
                break;
            case ChannelType.Ch2:
                resCh.SoundChannelPtr = SndChPtrNum.SND_CH2_PTR;
                resCh.Data.Main.AddOpcode(new CmdPanning { Pan = 0x22 });
                break;
            case ChannelType.Ch3:
                resCh.SoundChannelPtr = SndChPtrNum.SND_CH3_PTR;
                resCh.Data.Main.AddOpcode(new CmdPanning { Pan = 0x44 });
                resCh.Data.Main.AddOpcode(new CmdWaveVol { Vol = 0xC0 });
                resCh.Data.Main.AddOpcode(new CmdWaveCutoff { Length = 0 });
                if (waveMap.Count > 0)
                {
                    initialWave = new CmdWave { WaveId = waveMap.First().Value + 1 };
                    resCh.Data.Main.AddOpcode(initialWave);
                }
                break;
            case ChannelType.Ch4:
                resCh.SoundChannelPtr = SndChPtrNum.SND_CH4_PTR;
                resCh.Data.Main.AddOpcode(new CmdPanning { Pan = 0x88 });
                break;
        }

        // Determines which patterns are used only once (read: should be inlined)
        var canInline = chData.TrackOrder.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count() == 1);
        var songSpeed = song.Speed.Rounded;
        var noiseLsfr = 0; // For SetTimbre on Ch4, but this is not a safe command to use

        // Detect if the song uses vibratos or not.
        // If it doesn't, we can save CPU time by skipping vibrato commands.
        var usesVibratos = false;
        foreach (var track in chData.Tracks)
            foreach (var row in track.Value.Rows)
                if (row.Instrument.HasValue && vibratoMap.ContainsKey(row.Instrument.Value))
                {
                    usesVibratos = true;
                    goto vibratoCheckDone;
                }
            vibratoCheckDone:

        // If this is a sound effect with high priority, set appropriate parameter to chan_stop
        var priorityType = sheetSong.Priority == SongPriority.High
            ? (chData.Channel == ChannelType.Ch4 ? PriorityGroup.SNP_SFX4 : PriorityGroup.SNP_SFXMULTI)
            : (PriorityGroup?)null;

        var funcs = new Dictionary<int, SndFuncWrap>();
        foreach (var i in chData.TrackOrder.Distinct())
        {
            var func = new SndFunc { Parent = resCh.Data };
            var funcWrap = new SndFuncWrap
            {
                Func = func,
            };

            // Process rows
            var lastNote = (ICmdNote?)null;
            var lastNoteBak = (ICmdNote?)null; // Backup for inspection
            var macroLength = (int?)null;
            var queueSilence = false;
            int? lastVibrato = -1;
            int? lastWaveId = null;

            if (!chData.Tracks.TryGetValue(i, out var pattern))
            {
                Console.WriteLine($"INFO: Pattern Ch{((int)chData.Channel + 1)}.{i} is missing.");
                IncWaited(songSpeed * song.RowsPerTrack);
            }
            else
            {
                var j = 0;
                foreach (var row in pattern.Rows)
                {
                    // If a (wave) row specifies an instrument, immediately get the new cutoff length value
                    if (row.Instrument.HasValue)
                    {
                        macroLength = macroLenMap.GetValueOrDefault(row.Instrument.Value, null);

                        // Instrument ID also determines wave ID
                        if (chData.Channel == ChannelType.Ch3)
                            if (row.Instrument.HasValue)
                                if (sheetSong.Module.Instruments.TryGetValue(row.Instrument.Value, out var instr))
                                    if (instr.Channel == ChannelType.Ch3)
                                        if (instr.EnvelopeEnabled == true)
                                            if (instr.Envelope.HasValue)
                                                if (waveMap.TryGetValue(instr.Envelope.Value, out var waveId))
                                                    if (waveId != lastWaveId)
                                                    {
                                                        AddSpecOpcode(new CmdWave { WaveId = waveId + 1 });
                                                        lastWaveId = waveId;
                                                    }
                    }

                    // Base length for this row.
                    // Most of the time, this will be added as-is to the previous wait.
                    var noteLength = songSpeed;
                    var silenceLength = 0;

                    foreach (var x in row.Effects)
                    {
                        switch (x.EffectType)
                        {
                            case EffectType.PatternHalt:
                                // Halt immediately on the spot
                                funcWrap.StopCh = true;
                                goto exitPatternLoop;
                            case EffectType.SetTempo:
                                // Usually: noteLength = songSpeed, silenceDone = 0, silenceLength = 0
                                var silenceDone = songSpeed - noteLength;
                                songSpeed = new PrettySongSpeed(x.EffectParam).Rounded;
                                noteLength = songSpeed - silenceDone - silenceLength;
                                break;
                            case EffectType.Sfx when chData.Channel == ChannelType.Ch3: // wave
                                AddSpecOpcode(new CmdWaveCutoff { Length = x.EffectParam });
                                break;
                            case EffectType.Sfx when chData.Channel == ChannelType.Ch4: // noise
                                Console.WriteLine("noise_cutoff not implemented.");
                                break;
                            case EffectType.SetEnvelope when chData.Channel == ChannelType.Ch3:
                                if (waveMap.TryGetValue(x.EffectParam, out var waveId))
                                    if (waveId != lastWaveId)
                                    {
                                        lastWaveId = waveId;
                                        AddSpecOpcode(new CmdWave { WaveId = waveId + 1 });
                                    }
                                break;
                            case EffectType.SetEnvelope:
                                AddSpecOpcode(new CmdEnvelope { Arg = x.EffectParam });
                                break;
                            case EffectType.SetTimbre when chData.Channel == ChannelType.Ch3:
                                AddSpecOpcode(new CmdWaveVol { Vol = x.EffectParam << 6 });
                                break;
                            case EffectType.SetTimbre when chData.Channel == ChannelType.Ch4:
                                noiseLsfr = x.EffectParam << 3;
                                break;
                            case EffectType.SetTimbre:
                                AddSpecOpcode(new CmdDutyCycle { Duty = x.EffectParam });
                                break;
                            case EffectType.SetPanning:
                                // 00 (mute), 01 (left), 10 (right), 11 (both)
                                var l = x.EffectParam & 0b1;
                                var r = x.EffectParam >> 1;
                                AddSpecOpcode(new CmdPanning { Pan = (l << ((int)chData.Channel + 4)) + (r << (int)chData.Channel) });
                                break;
                            case EffectType.SetSweep:
                                AddSpecOpcode(new CmdSweep { Arg = x.EffectParam });
                                break;
                            case EffectType.DelayedCut:
                                // Row has note + silence
                                noteLength = x.EffectParam;
                                silenceLength = songSpeed - x.EffectParam;
                                break;
                            case EffectType.DelayedNote:
                                // Row has silence + note
                                IncWaited(x.EffectParam);
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
                            case EffectType.Vibrato: // This kind of Vibrato isn't supported, use instruments instead
                                break;
                            case EffectType.VibratoDelay:
                                break;
                            case EffectType.Tuning:
                                AddSpecOpcode(new CmdFineTuneValue { Offset = x.EffectParam - 0x80 });
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
                            AddSilence(noteLength);
                            break;
                        case 0:
                            if (noteLength > 0)
                                IncWaited(noteLength);
                            break;
                        default:
                            if (usesVibratos)
                            {
                                var curVib = vibratoMap.ContainsKey(row.Instrument.GetValueOrDefault()) ? vibratoMap[row.Instrument.GetValueOrDefault()] : (int?)null;
                                if (curVib != lastVibrato)
                                {
                                    func.AddOpcode(curVib.HasValue ? new CmdVibratoOp { VibratoId = curVib.Value } : new CmdClrVibrato());
                                    lastVibrato = curVib;
                                }
                            }

                            if (chData.Channel == ChannelType.Ch4)
                            {
                                int noteId;
                                if (row.Note >= SciNote.NoiseNoteTable.Length)
                                {
                                    Console.WriteLine($"WARNING: Row Ch{((int)chData.Channel + 1)}.{i}.{j} contains out of range note ID {row.Note}, replacing with max value.");
                                    noteId = SciNote.NoiseNoteTable[^1] + noiseLsfr;
                                }
                                else
                                {
                                    noteId = SciNote.NoiseNoteTable[row.Note - 1] + noiseLsfr;
                                }
                                lastNote = new CmdNoisePoly { Note = SciNote.CreateFromNoise(noteId), Length = noteLength };
                                func.AddOpcode((CmdNoisePoly)lastNote);
                            }
                            else
                            {
                                lastNote = new CmdNote { Note = SciNote.Create(row.Note), Length = noteLength };
                                func.AddOpcode((CmdNote)lastNote);
                                queueSilence = macroLength.HasValue && chData.Channel == ChannelType.Ch3;
                            }
                            break;
                    }


                    // Delayed cut
                    if (silenceLength > 0)
                        AddSilence(silenceLength);

                    // Delayed effects allow the current note to play (with the necessary automatic delay)
                    if (row.DelayedEffect.HasValue)
                    {
                        var x = row.DelayedEffect.Value;
                        switch (x.EffectType)
                        {
                            case EffectType.PatternGoto:
                                funcWrap.TerminatorGoto = x.EffectParam;
                                goto exitPatternLoop;
                            case EffectType.PatternSkip:
                                funcWrap.TerminatorSkip = x.EffectParam;
                                goto exitPatternLoop;
                        }
                    }
                    j++;
                }
            }
            void AddSilence(int length)
            {
                if (chData.Channel == ChannelType.Ch4)
                {
                    AddSpecOpcode(new CmdEnvelope()); // "envelope $00", for silence
                    lastNote = new CmdExtendNote { Length = length };
                    func.AddOpcode((CmdExtendNote)lastNote);
                }
                else
                {
                    lastNote = new CmdNote { Length = length };
                    func.AddOpcode((CmdNote)lastNote);
                }
            }

            void AddSpecOpcode(SndOpcode op)
            {
                if (lastNote != null)
                {
                    lastNoteBak = lastNote;
                    lastNote = null;
                }
                func.AddOpcode(op);
            }

            void IncWaited(int amount)
            {
                if (lastNote != null)
                {
                    // Continue the previous wait (usually the CmdNote's length)
                    lastNote.Length += amount;
                }
                else
                {
                    if (lastNoteBak == null || chData.Channel == ChannelType.Ch4)
                    {
                        // Waiting at the start of a subroutine
                        // or the previous note was on the noise channel
                        lastNote = new CmdExtendNote { Length = amount };
                        func.AddOpcode((CmdExtendNote)lastNote);
                    }
                    else
                    {
                        // everything else restarts the note
                        lastNote = new CmdWait { Length = amount };
                        func.AddOpcode((CmdWait)lastNote);
                    }
                }

                // If we passed over the limit, cap the lastNote length and move the remainder to a new silence note
                if (queueSilence && lastNote.Length > macroLength)
                {
                    queueSilence = false;
                    var newWait = new CmdNote { Length = lastNote.Length - macroLength };
                    func.AddOpcode(newWait);
                    lastNote.Length = macroLength;
                    lastNote = newWait;
                }
            }

        exitPatternLoop:

            // Optimization round
            // Delete duplicate consecutive wait lengths.
            // Not applicable on the noise channel, every wait matters there
            var lastLen = -1;
            if (chData.Channel != ChannelType.Ch4)
                foreach (var op in func.Opcodes.OfType<ICmdNote>())
                {
                    Debug.Assert(op.Length != null);
                    if (op.Length == lastLen && (op is CmdNote || op is CmdNoisePoly))
                        op.Length = null; // combo
                    else
                        lastLen = op.Length.Value;
                }

            funcs.Add(i, funcWrap);
        }
        

        // Optimization round
        // If only one wave ID is used, delete all refs and replace them with a single one.
        // Could be improved in the future by emulating the command handler and keeping track of wave changes that matter.
        if (chData.Channel == ChannelType.Ch3 && initialWave != null)
        {
            var allWaveChg = funcs.SelectMany(x => x.Value.Func.Opcodes.OfType<CmdWave>()).ToArray();
            var grp = allWaveChg.GroupBy(x => x.WaveId).ToArray();
            if (grp.Length == 1 && grp[0].Key == initialWave.WaveId)
            {
                foreach (var x in allWaveChg)
                    x.Parent.Opcodes.Remove(x);
                initialWave.WaveId = allWaveChg[0].WaveId;
            }
        }

        // Build the main function
        var doneOutput = new HashSet<int>();

        // This maps to the .P1 / .P2 / ... local labels in the main func
        // Jumps don't go forward so we should be fine adding to this as we go.
        var funcStartOpcodes = new Dictionary<int, SndOpcode>(); // <pattern id, jump target>

        for (var i = 0; i < chData.TrackOrder.Length; i++)
        {
            var trackId = chData.TrackOrder[i];
            if (!funcs.TryGetValue(trackId, out var pat))
            {
                // Hit a skipped line
            }
            else if (canInline[trackId])
            {
                funcStartOpcodes[i] = pat.Func.Opcodes[0];
                funcStartOpcodes[i].Label = $".P{i:X2}";

                // This inline branch assumes that inlined patterns are only called once.
                // If that's not the case, the following will happen:
                // - Duplicate terminator commands to the pattern
                // - Lost opcodes from duplicate object instances

                if (pat.StopCh)
                    pat.Func.AddOpcode(new CmdChanStop { Priority = priorityType });
                else if (pat.TerminatorGoto.HasValue)
                {
                    if (funcStartOpcodes.TryGetValue(pat.TerminatorGoto.Value, out var target))
                        pat.Func.AddOpcode(new CmdLoop { Target = target });
                    else
                        Console.WriteLine($"CmdLoop failure: {pat.TerminatorGoto} (limit: {chData.TrackOrder.Length})");
                }
                else if (pat.TerminatorSkip.GetValueOrDefault() != 0) // PatternSkip 0 is ret (and ret is ignored on inlined funcs)
                    Console.WriteLine("PatternSkip > 0 not implemented");

                // Reassign all opcodes inside to Main
                foreach (var x in pat.Func.Opcodes)
                    resCh.Data.Main.AddOpcode(x);
            }
            else
            {
                var callOp = new CmdCall { Target = pat.Func.Opcodes[0] };
                funcStartOpcodes[i] = callOp;
                funcStartOpcodes[i].Label = $".P{i:X2}";
                resCh.Data.Main.AddOpcode(callOp);

                if (doneOutput.Add(trackId))
                {
                    resCh.Data.Subs.Add(pat.Func);

                    if (pat.StopCh)
                        pat.Func.AddOpcode(new CmdChanStop { Priority = priorityType });
                    else if (pat.TerminatorGoto.HasValue && pat.TerminatorGoto.Value != chData.TrackOrder[0])
                        Console.WriteLine("PatternGoto not pointing to top is not allowed on patterns used more than once.");
                    else if (pat.TerminatorSkip.GetValueOrDefault() != 0) // PatternSkip 0 is ret
                        Console.WriteLine("PatternSkip > 0 not implemented");
                    else
                        pat.Func.AddOpcode(new CmdRet());
                }
            }
        }

        // If the last pattern doesn't end in CmdLoop or CmdChanStop, add either to the first pattern at the end of main
        if (funcStartOpcodes.Count > 0)
        {
            if (funcs.TryGetValue(chData.TrackOrder.Last(), out var lastPat))
            {
                if (lastPat.Func.Opcodes.Last() is not CmdLoop && !lastPat.StopCh)
                { 
                    if (sheetSong.Kind == SongKind.BGM)
                    {
                        Console.WriteLine($"Ch{((int)chData.Channel + 1)}: Last pattern doesn't end with Loop/ChanStop, added Loop to start.");
                        resCh.Data.Main.AddOpcode(new CmdLoop { Target = funcStartOpcodes.First().Value });
                    }
                    else
                    {
                        Console.WriteLine($"Ch{((int)chData.Channel + 1)}: Last pattern doesn't end with Loop/ChanStop, added ChanStop.");
                        resCh.Data.Main.AddOpcode(new CmdChanStop { Priority = priorityType });
                    }
                }
            }
            else
                Console.WriteLine($"lastPat failure: {chData.TrackOrder.Last()}");
        }

        res.Channels.Add(resCh);
    }

}
