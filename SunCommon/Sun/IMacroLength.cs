namespace SunCommon;

/// <summary>For commands whose macros support a length parameter.</summary>
public interface IMacroLength
{
    int? Length { get; set; }
}
