using SunCommon;

namespace SunDis;

public class CmdLoopCntEx : CmdLoopCnt, IHasPointerEx
{
    public required GbPtr TargetPtr { get; set; }
}