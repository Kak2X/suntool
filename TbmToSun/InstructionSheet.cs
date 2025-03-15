using SunCommon;

namespace TbmToSun
{
    public class InstructionSheet
    {
        public readonly string OutputPath;
        public readonly int? SplitOn;
        public readonly List<InstructionSong> Rows = [];
        public InstructionSheet(string file)
        {
            var cmds = File.ReadAllLines(file);
            for (var i = 0; i < cmds.Length; i++)
            {
                var cmd = cmds[i].Trim();
                if (!cmd.StartsWith(';'))
                {
                    cmd = cmd.Split(';')[0];
                    if (cmd.StartsWith("OutputPath="))
                        OutputPath = cmd.Split('=')[1];
                    else if (cmd.StartsWith("SplitOn="))
                        SplitOn = int.Parse(cmd.Split('=')[1]);
                    else
                    {
                        var row = cmds[i].Split(",").Select(x => x.Trim()).ToArray();
                        if (row.Length > 1)
                        {
                            Console.WriteLine($"-> {row[0]}");
                            using var input = new FileStream(row[0], FileMode.Open, FileAccess.Read);
                            Rows.Add(new InstructionSong
                            {
                                Path = row[0],
                                Module = new TbmModule(input),
                                Kind = row[1].ToUpperInvariant() switch
                                {
                                    "B" => SongKind.BGM,
                                    "S" => SongKind.SFX,
                                    "P" => SongKind.Pause,
                                    "U" => SongKind.Unpause,
                                    _ => 0,
                                },
                                Title = row.Length > 2 ? row[2] : Path.GetFileNameWithoutExtension(row[0]),
                            });
                        }
                    }
                }
            }
            if (OutputPath == null || Rows.Count == 0)
                throw new FormatException("Malformed output file");
        }
    }
    public class InstructionSong
    {
        public required string Path;
        public required TbmModule Module;
        public SongKind Kind;
        public string? Title;
    }
}
