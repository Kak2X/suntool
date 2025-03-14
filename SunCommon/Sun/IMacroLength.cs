namespace SunCommon;

/// <summary>For commands whose macros support compressing the next wait on.</summary>
public interface IMacroLength
{
    int? Length { get; set; }
}