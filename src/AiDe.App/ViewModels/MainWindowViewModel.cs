namespace AiDe.App.ViewModels;

public sealed class MainWindowViewModel
{
    public string WindowTitle => "AI-DE";

    public string Heading => "AI-DE desktop workspace";

    public IReadOnlyList<string> GettingStartedSteps { get; } =
    [
        "Add application features under src/AiDe.App.",
        "Keep presentation logic in view models for fast unit testing.",
        "Run dotnet test before publishing changes."
    ];

    public string StatusMessage => "Ready for local development.";
}
