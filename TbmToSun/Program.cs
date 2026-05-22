using SunCommon;
using TbmToSun;

if (args.Length == 0)
{
    Console.WriteLine(
@"TbmToSun:
  Converts TrackerBoy TBM modules to the ""Sun L"" sound driver format.
Usage:
  TbmToSun <path to sheet file>");
}
else
{
    var sheetPath = args[0];
    if (!Path.Exists(sheetPath))
        Console.WriteLine($"\"{sheetPath}\" does not exist.");
    else
    {
        Console.WriteLine($"Parsing sheet file & TBM files...");
        var sheet = new InstructionSheet(sheetPath);

        Console.WriteLine($"Converting...");
        var res = new TbmConversion(sheet);
        var extraSize = res.Vibratos.FullSizeInRom() + res.Waves.FullSizeInRom();

        Console.WriteLine($"Writing the disassembly files to \"{sheet.OutputPath}\"...");

        if (res.PtrTbl.Songs.Count > 0x7E)
            Console.WriteLine($"WARNING: Went over the song limit ({res.PtrTbl.Songs.Count}/{0x7E}), the converted files won't build.");

        using var sw = new MultiWriter(sheet.OutputPath);
        var writer = new DataWriter(sw, res.PtrTbl, (sheet.SplitOn ?? Consts.FreeSpaceBase) - extraSize, sheet.StartingBank);
        writer.WriteDisassembly();

        Console.WriteLine($"Writing vibratos...");
        res.Vibratos.WriteToDisasm(sw);
        Console.WriteLine($"Writing waves...");
        res.Waves.WriteToDisasm(sw);

        Console.WriteLine("Done.");
    }
}