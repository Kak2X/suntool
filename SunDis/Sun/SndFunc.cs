using SunCommon;
namespace SunDis;

public class SndFunc : RomData
{
    public readonly List<SndOpcode> Opcodes = [];

    public SndFunc(Stream s, SongInfo song, FormatOptions opt) : base(s)
    {
        var parser = OpcodeParser.Create(opt);
        song.LoopCount = 0;

        SndOpcode cmd;
        do
        {
            cmd = parser.Parse(s, song);
            Opcodes.Add(cmd);
        } while (!cmd.Terminates);

        // Self-check that all non-call pointers point inside the function.
        long min = Location.RomAddress, max = s.Position;
        foreach (var x in Opcodes.OfType<IHasPointer>())
            if (x is not CmdCall && (x.Target.RomAddress < min || x.Target.Address >= max))
                throw new Exception($"Unsupported jump pointer outside function at 0x{x.Target.RomAddress:X08}");
    }

    public override int SizeInRom() => 0;

    public override void WriteToDisasm(MultiWriter sw)
    {
    }
}
