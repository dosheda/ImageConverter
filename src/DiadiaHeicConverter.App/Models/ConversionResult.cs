namespace DiadiaHeicConverter.App.Models;

public sealed record ConversionResult(
    ConversionStatus Status,
    string? OutputPath = null,
    string? ErrorMessage = null)
{
    public static ConversionResult Success(string outputPath) =>
        new(ConversionStatus.Succeeded, outputPath);

    public static ConversionResult Failure(string errorMessage) =>
        new(ConversionStatus.Failed, null, errorMessage);

    public static ConversionResult Cancelled() =>
        new(ConversionStatus.Cancelled, null);
}
