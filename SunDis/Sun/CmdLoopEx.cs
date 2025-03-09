using SunCommon;

namespace SunDis;

public class CmdLoopEx : CmdLoop, IHasPointerEx
{
    public required GbPtr TargetPtr { get; set; }
}