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