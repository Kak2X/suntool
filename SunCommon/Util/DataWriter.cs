using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SunCommon;

public class DataWriter
{
    protected readonly IMultiWriter W;
    protected readonly PointerTable PtrTbl;
    public DataWriter(IMultiWriter w, PointerTable ptrTbl)
    {
        W = w;
        PtrTbl = ptrTbl;
    }

    public void WriteDisassembly()
    {
        OptimizeLength();
        NumberTargets();

        W.ChangeFile("driver/data/song_list.asm");
        W.WriteWithLabel(PtrTbl);

        var files = OnParseSongs();

        // TODO: Autodistribute, with a base count configurable (with default value)
        W.ChangeFile("driver/song_includes.asm");
        W.Write(Consts.SndMainBegin);
        foreach (var x in files)
            W.WriteLine($"INCLUDE \"{x.Replace('\\', '/')}\"");
    }

    protected virtual List<string> OnParseSongs()
    {
        var files = new List<string>();
        // For each song...
        foreach (var song in PtrTbl.Songs)
        {
            // Put it in a separate file
            var songPath = $"driver/{(song.IsSfx ? "sfx/" : "bgm/")}{song.Name}.asm".ToLowerInvariant();
            W.ChangeFile(songPath);
            files.Add(songPath);

            // Song header
            W.WriteWithLabel(song);
            foreach (var chan in song.Channels)
                W.WriteWithLabel(chan);

            // Song data
            foreach (var chan in song.Channels)
            {
                W.WriteWithLabel(chan.Data.Main);
                foreach (var op in chan.Data.Main.Opcodes)
                    W.WriteWithLabel(op);

                foreach (var sub in chan.Data.Subs)
                {
                    W.WriteWithLabel(sub);
                    foreach (var op in sub.Opcodes)
                        W.WriteWithLabel(op);
                }
            }
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