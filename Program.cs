using SunnyDay;
using System.Diagnostics;


// TODO: LABEL CALLS and LOOPS (requires keeping count of local sub count & loop count, checking if a label has been assigned already to skip duplicates)

//var src = "0D:4B9C"; // "0D:4B08"
//var bnk = "0D:4C5E"; // "0D:4C14"
//var songCount = 1; // 76
{
    /*
        var src = "1F:46A5";
    var bnk = default(string);
    var songCount = 52;
    var format = DataMode.KOF96;
    */

    var src = "0D:4B08";
    var bnk = "0D:4C14";
    var songCount = 76;
    var format = DataMode.OP;
    using (var fs = new FileStream("original.gb", FileMode.Open))
    {
        fs.Seek(GbPtrPool.Create(src).RomAddress, SeekOrigin.Begin);
        var test = new PointerTable(fs, songCount, bnk != null ? GbPtr.Create(bnk) : null, new FormatOptions { Mode = format });
        var dump = test.GetRomDump(GapMode.TryDecode);
        dump.ToDisassembly("C:\\soundtemp");
    }
}


return;
if (args.Length == 0)
{
    Console.WriteLine(
     @"
sunny:
  Disassembles data from the Sun L sound drivers.
Usage:
  sunny [options]
Options:
  --rom           The path to the ROM file that is to be parsed.
  --output        Path to the output folder.
                  This will be created, if missing.
  --format        Song format. (96 or OP)
  --ptr-tbl       Location of the sound header pointer table with song data.
                  This can be formatted as an hex ROM address, or in the BANK:ADDR format.
 [--bank-tbl]     Location of the table containing song bank number.
                  Only required for versions of the driver which use bankswitching.
  --count         Number of songs to read from the pointer table.
");
} 
else
{
    var xargs = new KArgs(args);

    var sourceFile = xargs.Get("rom", true);
    var outputPath = xargs.Get("output", true);
    var format = xargs.Get("format", true).ToUpperInvariant() switch
    {
        "96" => DataMode.KOF96,
        "OP" => DataMode.OP,
        _ => throw new Exception("Improper --format parameter."),
    };
    var ptrTable = GbPtrPool.Create(xargs.Get("ptr-tbl", true));
    var songCount = int.Parse(xargs.Get("count", true));
    var bankTbl = format == DataMode.OP ? GbPtrPool.Create(xargs.Get("bank-tbl", true)) : null;

    using (var fs = new FileStream(sourceFile, FileMode.Open))
    {
        fs.Seek(ptrTable.RomAddress, SeekOrigin.Begin);
        var root = new PointerTable(fs, songCount, bankTbl, new FormatOptions { Mode = format });
        var dump = root.GetRomDump(GapMode.TryDecode);
        dump.ToDisassembly(outputPath);
    }

    // TODO: Go down through the RomData tree, adding it inside the HashSet
    // Order them by pointer
    // Call one of their output functions (ie: romdata.toDisasm()) and make sure to check for gaps with the size prop.
}



class KArgs
{
    private Dictionary<string, List<string>> _args;
    public KArgs(string[] args)
    {
        _args = [];

        string curKey = null!;
        List<string> curVal = [];
        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                if (curKey != null)
                    _args.Add(curKey, curVal);
                curKey = arg[2..];
                curVal = [];
            }
            else
                curVal.Add(arg);
        }
        if (curKey != null)
            _args.Add(curKey, curVal);
    }

    public bool Exists(string key)
    {
        return _args.ContainsKey(key);
    }

    public string? Get(string key, bool required = false)
    {
        return GetMulti(key, required).FirstOrDefault();
    }

    public List<string> GetMulti(string key, bool required = false)
    {
        if (_args.TryGetValue(key, out var val))
        {
            if (required && val.Count == 0)
                throw new KeyNotFoundException($"Parameter --{key} requires arguments.");
            return val;
        } 
        else if (required)
            throw new KeyNotFoundException($"Parameter --{key} is required.");
        else
            return [];
    }
}