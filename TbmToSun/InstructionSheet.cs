using SunCommon;

namespace TbmToSun
{
    public class InstructionSheet
    {
        public readonly DataMode OutputFormat;
        public readonly string OutputPath;
        public readonly List<InstructionSong> Rows = [];
        public InstructionSheet(string file)
        {
            var cmds = File.ReadAllLines(file);
            for (int i = 0, rowNum = 0; i < cmds.Length; i++)
            {
                var cmd = cmds[i].Trim();
                if (!cmd.StartsWith(';'))
                {
                    if (rowNum == 0)
                        OutputFormat = Enum.Parse<DataMode>(cmd);
                    else if (rowNum == 1)
                        OutputPath = cmd;
                    else
                    {
                        var row = cmds[i].Split(",").Select(x => x.Trim()).ToArray();
                        if (row.Length > 1)
                        {
                            using var input = new FileStream(row[0], FileMode.Open, FileAccess.Read);
                            Rows.Add(new InstructionSong
                            {
                                Module = new TbmModule(input),
                                IsSfx = int.Parse(row[1]) != 0,
                                Title = row.Length > 2 ? row[2] : null
                            });
                        }
                    }
                    rowNum++;
                }
            }
            if (OutputPath == null || Rows.Count == 0)
                throw new FormatException("Malformed output file");
        }
    }
    public class InstructionSong
    {
        public required TbmModule Module;
        public bool IsSfx;
        public string? Title;
    }
}
