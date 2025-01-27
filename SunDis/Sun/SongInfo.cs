namespace SunDis;

public class SongInfo
{
    public SongInfo(int id)
    {
        Id = id;
    }

    public int Id;
    public bool IsSfx;
    public int ChNum;
    public int LoopCount;

    public string TypeString => IsSfx ? "SFX" : "BGM";
    private SndChPtrNum _chPtr;
    public SndChPtrNum ChPtr
    {
        get => _chPtr;
        set
        {
            _chPtr = value;
            ChNum = value.Normalize();
        }
    }
}
