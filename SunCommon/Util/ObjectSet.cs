namespace SunCommon;

/// <summary>
///     Object list containing unique object instances
/// </summary>
public class ObjectSet
{
    private readonly List<object> _list = [];
    public bool Add(object obj)
    {
        if (_list.Contains(obj))
            return false;
        _list.Add(obj);
        return true;
    }
}
