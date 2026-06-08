namespace STFU.UI.Bridge.Scene;

public sealed record SceneDiagnosticItem(
    string Severity,
    string Message,
    string Detail)
{
    public string DisplayText => string.IsNullOrWhiteSpace(Detail)
        ? $"{Severity}: {Message}"
        : $"{Severity}: {Message} - {Detail}";
}
