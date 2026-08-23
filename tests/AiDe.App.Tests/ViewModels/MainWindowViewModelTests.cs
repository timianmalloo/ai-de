using AiDe.App.ViewModels;

namespace AiDe.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_ProvidesStarterContent()
    {
        var viewModel = new MainWindowViewModel();

        Assert.Equal("AI-DE", viewModel.WindowTitle);
        Assert.Equal("AI-DE desktop workspace", viewModel.Heading);
        Assert.NotEmpty(viewModel.GettingStartedSteps);
        Assert.Equal("Ready for local development.", viewModel.StatusMessage);
    }
}
