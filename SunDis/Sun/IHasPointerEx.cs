using SunCommon;

namespace SunDis;

public interface IHasPointerEx : IHasPointer
{
    GbPtr TargetPtr { get; set; }
}