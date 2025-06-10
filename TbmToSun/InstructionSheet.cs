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
            var inputPath = "";
            for (var i = 0; i < cmds.Length; i++)
            {
                var cmd = cmds[i].Trim();
                if (!cmd.StartsWith(';'))
                {
                    cmd = cmd.Split(';')[0]; // Remove everything to the right of the first ;
                    if (cmd.StartsWith("OutputPath="))
                        OutputPath = cmd.Split('=')[1];
                    else if (cmd.StartsWith("InputPath="))
                        inputPath = cmd.Split('=')[1];
                    else if (cmd.StartsWith("SplitOn="))
                        SplitOn = int.Parse(cmd.Split('=')[1]);
                    else
                    {
                        var row = cmds[i].Split(",").Select(x => x.Trim()).ToArray();
                        if (row.Length > 1)
                        {
                            var fullPath = Path.Combine(inputPath, row[0]);
                            Console.WriteLine($"-> {fullPath}");
                            if (Directory.Exists(fullPath))
                            {
                                var found = Directory.GetFiles(fullPath, "*.tbm", SearchOption.AllDirectories);
                                foreach (var x in found)
                                    AddFile(x, row);
                            }
                            else if (!File.Exists(fullPath))
                                Console.WriteLine($"!! This file doesn't exist.");
                            else
                                AddFile(fullPath, row);
                        }
                    }
                }
            }
            if (OutputPath == null || Rows.Count == 0)
                throw new FormatException("Malformed output file");
        }

        private void AddFile(string fullPath, string[] row)
        {
            try
            {
                using var input = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                Rows.Add(new InstructionSong
                {
                    Path = fullPath,
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
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
