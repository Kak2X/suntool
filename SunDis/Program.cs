using SunCommon;
using SunDis;
using System.IO;

if (args.Length == 0)
{
    Console.WriteLine(
@"SunDis:
  Disassembles data from the Sun L sound drivers.
Usage:
  SunDis [options]
Options:
  --rom           The path to the ROM file that is to be parsed.
  --output        Path to the output folder.
                  This will be created, if missing.
  --format        Song format. (95, 96, OP or EX)
  --output-format Song format. (95, 96, OP or EX)
  --ptr-tbl       Location of the sound header pointer table with song data.
                  This can be formatted as an hex ROM address, or in the BANK:ADDR format.
 [--bank-tbl]     Location of the table containing song bank number. (OP only)
                  If not specified, it's assumed to come right after the pointer table.
  --count         Number of songs to read from the pointer table.");
} 
else
{
    var xargs = new KArgs(args);

    var sourceFile = xargs.Get("rom", true)!;
    var outputPath = xargs.Get("output", true)!;
    var format = xargs.Get("format", true)!.ToUpperInvariant() switch
    {
        "95" => DataMode.KOF95,
        "96" => DataMode.KOF96,
        "OP" => DataMode.OP,
        "EX" => DataMode.OPEx,
        _ => throw new Exception("Improper --format parameter."),
    };
    var ptrTable = GbPtrPool.Create(xargs.Get("ptr-tbl", true)!);
    var songCount = int.Parse(xargs.Get("count", true)!);
    var bankTbl = (GbPtr?)null;
    if (format == DataMode.OP)
    {
        var x = xargs.Get("bank-tbl");
        if (x != null)
            bankTbl = GbPtrPool.Create(x);
    }

    using var fs = new FileStream(sourceFile, FileMode.Open);
    fs.Seek(ptrTable.RomAddress, SeekOrigin.Begin);

    var res = new GbReader(fs, songCount, bankTbl, new FormatOptions { Mode = format }).Read();
    // No Vibrato or Wave dump
    using var sw = new MultiWriter(outputPath);
    res.Playlist.Mode = DataMode.OPEx; // Force disassembly-friendly main.asm
    new SortedDataWriter(sw, res).WriteDisassembly();
}
