namespace SunCommon;

public class VibratoTable : IRomData
{
    public IReadOnlyList<VibratoDef> Vibratos => _vibratos;
    private readonly List<VibratoDef> _vibratos = [];
    private bool _optimized;

    public int SizeInRom() => 0;
    public string? GetLabel() => null;
    public void WriteToDisasm(IMultiWriter sw)
    {
        if (!_optimized)
            Optimize();
        Recount();

        sw.ChangeFile("driver/data/vibrato.asm");
        sw.WriteLine(Consts.VibratoTblBegin);
        foreach (var x in _vibratos)
            sw.WriteWithLabel(x);
        foreach (var x in _vibratos.Select(x => x.Data).Distinct())
            sw.WriteWithLabel(x);
    }

    private void Recount()
    {
        var done = new ObjectSet();
        var i = 0;
        foreach (var x in _vibratos)
            if (done.Add(x.Data))
                x.Data.Id = i++;
    }

    private void Optimize()
    {
        _optimized = true;

        var toCheck = _vibratos.Select(x => x.Data).Distinct().OrderBy(x => x.Offsets!.Length).ToList();
        for (var i = 0; i < toCheck.Count; i++)
        {
            // Needle length always <= haystack
            var needle = toCheck[i];
            for (var j = toCheck.Count - 1; j > i; j--)
            {
                var haystack = toCheck[j];
                if (haystack.Offsets.EndsWith(needle.Offsets))
                {
                    // Needle can be deleted
                    //i--; // Balance
                    //j--; // j always > i

                    // Repoint all uses (needle will become unreferenced)
                    var posDiff = haystack.Offsets.Length - needle.Offsets.Length;
                    for (var k = 0; k < _vibratos.Count; k++)
                    {
                        var def = _vibratos[k];
                        if (def.Data == needle)
                        {
                            def.Data = haystack;
                            def.StartPoint += posDiff;
                            def.Data.Parents.Add(def);
                        }
                    }
                }
            }
        }
    }

    public VibratoItem this[int index] => _vibratos[index].Data;
    public int Push(byte[] data, int loopPoint = 0)
    {
        if (data.Length == 0)
            throw new ArgumentException("Empty vibrato passed in.", nameof(data));
        if (loopPoint >= data.Length)
        {
            Console.WriteLine($"Vibrato has bad loop point. ({loopPoint} >= {data.Length}), adjusted to {data.Length-1}");
            loopPoint = data.Length - 1;
        }

        // Try find existing
        for (var i = 0; i < _vibratos.Count; i++)
            if (_vibratos[i].LoopPoint == loopPoint && data.SequenceEqual(_vibratos[i].Data.Offsets))
                return i;
       
        // Add if not found
        var toAdd = new VibratoDef { Id = _vibratos.Count, Data = new VibratoItem { Offsets = data }, LoopPoint = loopPoint };
        _vibratos.Add(toAdd);
        _optimized = false;

        return toAdd.Id;
    }

    public int FullSizeInRom()
    {
        var res = SizeInRom();
        var done = new ObjectSet();
        foreach (var x in _vibratos)
        {
            res += x.SizeInRom();
            if (done.Add(x.Data))
                res += x.Data.SizeInRom();
        }
        return res;
    }
}
