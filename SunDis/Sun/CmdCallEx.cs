using SunCommon;

namespace SunDis;

public class CmdCallEx : CmdCall, IHasPointerEx
{
    public required GbPtr TargetPtr { get; set; }
}