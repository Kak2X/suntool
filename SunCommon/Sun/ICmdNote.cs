namespace SunCommon;

/// <summary>For commands whose macros support a length parameter.</summary>
public interface ICmdNote
{
    int? Length { get; set; }
}