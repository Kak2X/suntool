using SunCommon;
using TbmToSun;

if (args.Length == 0)
{
    Console.WriteLine(
@"TbmToSun:
  Converts TrackerBoy TBM modules to the ""Sun L"" sound driver format.
Usage:
  TbmToSun [options]
Options:
  --i      The path to the instruction file.");
}
else
{
    var xargs = new KArgs(args);

    var instrPath = xargs.Get("i", true)!;
    if (!Path.Exists(instrPath))
        Console.WriteLine($"\"{instrPath}\" does not exist.");
    else
    {
        var instrFile = new InstructionSheet(instrPath);
        using var writer = new MultiWriter(instrFile.OutputPath);
        OpWriter.Write(instrFile.Rows[0].Module, writer, instrFile.Rows[0].Title, instrFile.Rows[0].IsSfx, "vibset");
    }
}