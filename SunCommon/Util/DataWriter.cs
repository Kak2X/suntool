using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SunCommon;

public class DataWriter
{
    protected readonly IMultiWriter W;
    protected readonly PointerTable PtrTbl;
    protected readonly int SplitOn;
    protected record SongDef(string FilePath, SndHeader Song);
    private class SongGroupDef
    {
        public required List<SongDef> Groups;
        public required int Size;
    }
    // Object list containing unique object instances
    private class ObjectSet
    {
        private List<object> _list = new();
        public bool Add(object obj)
        {
            if (_list.Contains(obj))
                return false;
            _list.Add(obj);
            return true;
        }
    }

    public DataWriter(IMultiWriter w, PointerTable ptrTbl, int splitOn)
    {
        W = w;
        PtrTbl = ptrTbl;
        SplitOn = splitOn;
    }

    public void WriteDisassembly()
    {
        OptimizeLength();
        NumberTargets();

        W.ChangeFile("driver/data/song_list.asm");
        W.WriteWithLabel(PtrTbl);

        var files = OnParseSongs();
        DistributeSongs(files);
    }

    protected virtual SortedDictionary<int, SongDef> OnParseSongs()
    {
        var files = new SortedDictionary<int, SongDef>();
        var done = new ObjectSet();
        // For each song...
        foreach (var song in PtrTbl.Songs)
        {
            // Put it in a separate file
            var songPath = $"driver/{(song.IsSfx ? "sfx/" : "bgm/")}{song.Name}.asm".ToLowerInvariant();
            W.ChangeFile(songPath);
            files.Add(song.Id, new SongDef(songPath, song));

            // Song header
            if (Write(song))
                foreach (var chan in song.Channels)
                    Write(chan);

            // Song data
            foreach (var chan in song.Channels)
            {
                if (Write(chan.Data.Main))
                    foreach (var op in chan.Data.Main.Opcodes)
                        Write(op);
                foreach (var sub in chan.Data.Subs)
                    if (Write(sub))
                        foreach (var op in sub.Opcodes)
                            Write(op);
            }
        }

        bool Write(IRomData data)
        {
            if (!done.Add(data))
                return false;
            W.WriteWithLabel(data);
            return true;
        }

        return files;
    }

    private void OptimizeLength()
    {
        foreach (var song in PtrTbl.Songs)
            foreach (var chan in song.Channels)
            {
                Optimize(chan.Data.Main);
                foreach (var sub in chan.Data.Subs)
                    Optimize(sub);
            }
    }
    private static void Optimize(SndFunc func)
    {
        for (var i = 1; i < func.Opcodes.Count; i++)
        {
            if (func.Opcodes[i - 1] is IMacroLength lastCmd && func.Opcodes[i] is CmdWait curCmd && curCmd.Length.HasValue)
            {
                lastCmd.Length = lastCmd.Length.GetValueOrDefault() + curCmd.Length.GetValueOrDefault();
                curCmd.Length = null;
            }
        }
    }

    private void NumberTargets()
    {
        // <automatic detect + >
        // [<func>][<jump num>]

        var mapFunc = new Dictionary<SndFunc, List<SndOpcode>>();
        var mapCall = new Dictionary<SndData, List<SndOpcode>>();

        // Clear everything first
        foreach (var song in PtrTbl.Songs)
            foreach (var chan in song.Channels)
            {
                Reset(chan.Data.Main);
                foreach (var sub in chan.Data.Subs)
                {
                    Reset(sub);
                }
            }

        void Reset(SndFunc func)
        {
            for (var i = 0; i < func.Opcodes.Count; i++)
            {
                func.Opcodes[i].JumpNum = null;
                func.Opcodes[i].CallNum = null;
                if (func.Opcodes[i] is IHasPointer op)
                {
                    Debug.Assert(op.Target != null);
                    // Remember the indexes are supposed to be relative to the TARGET
                    // This will account for cross channel/song calls
                    if (op is CmdCall)
                        mapCall.Push(op.Target.Parent.Parent!, op.Target);
                    else // CmdLoop or CmdLoopCnt
                        mapFunc.Push(op.Target.Parent, op.Target);
                }
            }
        }

        // Then assign the counters
        foreach (var calls in mapCall)
            for (var i = calls.Value.Count - 1; i >= 0; i--) // In reverse, to make lower IDs win
            {
                calls.Value[i].CallNum = i;
                calls.Value[i].Parent.SubId = i; // Needed for jumps so they know which label to use
            }

        foreach (var jumps in mapFunc)
            for (var i = jumps.Value.Count - 1; i >= 0; i--) // In reverse, same reason
            {
                jumps.Value[i].JumpNum = i;
                jumps.Value[i].CallNum = jumps.Value[i].Parent.SubId;
            }
    }


    private void DistributeSongs(SortedDictionary<int, SongDef> files)
    {
        W.ChangeFile("driver/song_includes.asm");
        if (SplitOn == 0) // Disabled
        {
            W.WriteIndent("mSOUNDBANK 03, 1");
            foreach (var x in files)
                W.WriteLine($"INCLUDE \"{x.Value.FilePath.Replace('\\', '/')}\"");
        }
        else
        {
            var realSplitOn = SplitOn - PtrTbl.SizeInRom();

            // Find the song dependencies
            var dependencies = new Dictionary<int, SortedSet<int>>(); // SongId, GroupId
            foreach (var def in files)
            {
                var set = new SortedSet<int>([def.Value.Song.Id]); // Obvious dependency on self
                foreach (var ch in def.Value.Song.Channels)
                {
                    // Both the sound channel and sound data can point to other songs
                    set.Add(ch.Parent.Id);
                    set.Add(ch.Data.Parent.Parent.Id);
                    // And the jump targets, too
                    foreach (var op in ch.Data.Main.Opcodes.OfType<IHasPointer>())
                        set.Add(op.Target!.Parent.Parent.Parent.Parent.Id);
                    foreach (var sub in ch.Data.Subs)
                        foreach (var op in sub.Opcodes.OfType<IHasPointer>())
                            set.Add(op.Target!.Parent.Parent.Parent.Parent.Id);
                }
                dependencies[def.Value.Song.Id] = set;
            }
            // Compress dependencies to groups. Songs in each group need to be stored in the same bank.
            var keys = dependencies.Keys.ToArray();
            for (var i = 0; i < keys.Length; i++)
            {
                var delSrcList = false;
                var srcList = dependencies[keys[i]];
                for (var j = i + 1; j < keys.Length; j++)
                {
                    var dstList = dependencies[keys[j]];
                    if (srcList.Intersect(dstList).Any())
                    {
                        foreach (var k in srcList)
                            dstList.Add(k);
                        delSrcList = true;
                    }
                }
                if (delSrcList)
                    dependencies.Remove(keys[i]);
            }
            // Create the final group counters
            var songGroups = dependencies.Select(x =>
            {
                var songs = x.Value.Select(y => files[y]).ToArray();
                // Must account for the cross-jumps & object instance equality, so another list of object it is.
                var totalSize = 0;
                var done = new ObjectSet();
                foreach (var song in songs)
                    if (AddSize(song.Song))
                        foreach (var ch in song.Song.Channels)
                            if (AddSize(ch)) // && AddSize(ch.Data))
                            {
                                if (AddSize(ch.Data.Main))
                                    foreach (var op in ch.Data.Main.Opcodes)
                                        AddSize(op);
                                foreach (var sub in ch.Data.Subs)
                                    if (AddSize(sub))
                                        foreach (var op in sub.Opcodes)
                                            AddSize(op);
                            }
                bool AddSize(IRomData data)
                {
                    if (!done.Add(data))
                        return false;
                    totalSize += data.SizeInRom();
                    return true;
                }

                if (totalSize > realSplitOn)
                    throw new InvalidOperationException($"Song group is {totalSize.AsHexWord()} bytes long, and will not fit with the split size of {realSplitOn.AsHexWord()}. Cannot continue.");
                return new SongGroupDef { Groups = x.Value.Select(y => files[y]).ToList(), Size = totalSize };
            });

            // Distribute the groups across banks. To make best use of the space available, they are sorted by length.
            var banks = new List<SongGroupDef>();
            foreach (var cur in songGroups.OrderByDescending(x => x.Size))
            {
                var foundExisting = false;
                foreach (var bank in banks)
                {
                    if (bank.Size + cur.Size <= realSplitOn)
                    {
                        bank.Size += cur.Size;
                        bank.Groups.AddRange(cur.Groups);
                        foundExisting = true;
                        break;
                    }
                }
                if (!foundExisting)
                    banks.Add(cur);
            }

            // Finally, print them out
            for (var i = 0; i < banks.Count; i++)
            {
                W.WriteIndent($"mSOUNDBANK {(banks.Count - i):00}{(i == 0 ? ", 1 ; First bank" : "")} ; Size: {banks[i].Size.AsHexWord()}");
                foreach (var x in banks[i].Groups)
                    W.WriteLine($"INCLUDE \"{x.FilePath.Replace('\\', '/')}\"");
            }
        }
    }
}
file static class Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Push<TKey, TVal>(this Dictionary<TKey, List<TVal>> map, TKey key, TVal value) where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
            map.Add(key, [value]);
        else if (!list.Contains(value))
            list.Add(value);
    }
}