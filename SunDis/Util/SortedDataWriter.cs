using SunCommon;
using System.Diagnostics;

namespace SunDis;

public class SortedDataWriter : DataWriter
{
    private readonly GbReaderResult Read;
    private readonly bool DbgAddr;

    public SortedDataWriter(IMultiWriter w, GbReaderResult read, bool dbgAddr = false) : base(w, read.Playlist)
    {
        Read = read;
        DbgAddr = dbgAddr;
    }

    protected override List<string> OnParseSongs()
    {
        // For this to work properly, labels should never be defined on 0-byte structures.
        // Unfortunately required in SndFunc, so there is a special case for it.
        var files = new List<string>();
        W.ChangeFile("driver/_initial_trash.asm");
        
        var lastParent = default(SndFunc);
        foreach (var obj in Read.Sorted)
        {
            if (obj.Data is SndHeader song)
            {
                // Put it in a separate file
                var songPath = $"driver/{(song.IsSfx ? "sfx/" : "bgm/")}{song.Name}.asm".ToLowerInvariant();
                W.ChangeFile(songPath);
                files.Add(songPath);
            }
            else if (obj.Data is SndOpcode sndOp && sndOp.Parent != lastParent)
            {
                lastParent = sndOp.Parent;
                W.WriteWithLabel(lastParent);
            }

            if (!DbgAddr)
            {
                W.WriteWithLabel(obj.Data);
            }
            else
            {
                W.Write(obj.Location.ToBankString() + " | ");
                W.WriteWithLabel(obj.Data);
                if (obj.Data is CmdWait wt && wt.Length == null)
                    W.Write("[optimized wait]\r\n");
                if (obj.Data is IHasPointerEx xxx)
                    W.Write(xxx.TargetPtr.ToBankString() + " ^ TARGET PTR\r\n");
            }

        }
        return files;
    }
}
