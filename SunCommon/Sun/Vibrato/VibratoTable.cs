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
        sw.WriteLine(Consts.VibratoTblBegin);
        foreach (var x in _vibratos)
            sw.WriteWithLabel(x);
        foreach (var x in _vibratos.Select(x => x.Data).Distinct())
            sw.WriteWithLabel(x);
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
                    // Mark as reference
                    //needle.RefId = j;
                    //needle.Offsets = null;

                    toCheck.RemoveAt(i);
                    i--; // Balance
                    j--; // j always > i

                    // Repoint all uses (needle will become unreferenced)
                    var pos = haystack.Offsets.Length - needle.Offsets.Length;
                    for (var k = 0; k < _vibratos.Count; k++)
                    {
                        if (_vibratos[k].Data == needle)
                        {
                            _vibratos[k].Data = haystack;
                            _vibratos[k].StartPoint += pos;
                            _vibratos[k].LoopPoint += pos;
                        }
                    }
                }
            }
        }
    }

    public VibratoItem this[int index] => _vibratos[index].Data;
    public int Push(byte[] data, int loopPoint = 0)
    {
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
}
