using SunCommon;
using SunDis;

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
  --format        Song format. (96 or OP)
  --ptr-tbl       Location of the sound header pointer table with song data.
                  This can be formatted as an hex ROM address, or in the BANK:ADDR format.
 [--bank-tbl]     Location of the table containing song bank number.
                  Only required for versions of the driver which use bankswitching.
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
        _ => throw new Exception("Improper --format parameter."),
    };
    var ptrTable = GbPtrPool.Create(xargs.Get("ptr-tbl", true)!);
    var songCount = int.Parse(xargs.Get("count", true)!);
    var bankTbl = format == DataMode.OP ? GbPtrPool.Create(xargs.Get("bank-tbl", true)!) : null;

    using var fs = new FileStream(sourceFile, FileMode.Open);
    fs.Seek(ptrTable.RomAddress, SeekOrigin.Begin);
    var root = new PointerTable(fs, songCount, bankTbl, new FormatOptions { Mode = format });
    var dump = root.GetRomDump(GapMode.TryDecode);
    dump.ToDisassembly(outputPath);
}
