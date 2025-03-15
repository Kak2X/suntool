using SunCommon;
using System.Diagnostics;

namespace SunDis;

public class SortedDataWriter : DataWriter
{
    private readonly GbReaderResult Read;
    private readonly bool DbgAddr;

    public SortedDataWriter(IMultiWriter w, GbReaderResult read, bool dbgAddr = false) : base(w, read.Playlist, 0)
    {
        Read = read;
        DbgAddr = dbgAddr;
    }

    protected override SortedDictionary<int, SongDef> OnParseSongs()
    {
        // For this to work properly, labels should never be defined on 0-byte structures.
        var files = new SortedDictionary<int, SongDef>();
        W.ChangeFile("driver/_initial_trash.asm");
        
        foreach (var obj in Read.Sorted)
        {
            if (obj.Data is SndHeader song)
            {
                // Put it in a separate file
                var songPath = $"driver/{(song.Kind == SongKind.SFX ? "sfx/" : "bgm/")}{song.Name}.asm".ToLowerInvariant();
                W.ChangeFile(songPath);
                files.Add(song.Id, new SongDef(songPath, song));
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
