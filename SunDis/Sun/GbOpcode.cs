using SunCommon;

namespace SunDis;

public class GbOpcode : GbData
{
    public GbOpcode(GbPtr loc, SndOpcode data) : base(loc, data)
    {
    }

    public SndOpcode Opcode => (SndOpcode)Data;
}
