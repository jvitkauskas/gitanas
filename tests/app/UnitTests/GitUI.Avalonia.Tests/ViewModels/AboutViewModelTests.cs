using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class AboutViewModelTests
{
    private readonly RecordingHost _host = new();

    [Test]
    public void ThanksToText_names_the_contributor_count_and_one_contributor()
    {
        AboutViewModel viewModel = CreateViewModel(["Alice", "Bob", "Carol"]);

        viewModel.ThanksToText.Should().MatchRegex(@"^Thanks to over 3 contributors: (Alice|Bob|Carol)$");
    }

    [Test]
    public void ThankNextContributor_picks_again()
    {
        AboutViewModel viewModel = CreateViewModel(["Alice", "Bob"], new Random(1));
        HashSet<string> seen = [];

        for (int i = 0; i < 20; i++)
        {
            viewModel.ThankNextContributor();
            seen.Add(viewModel.ThanksToText);
        }

        seen.Should().HaveCount(2);
    }

    [Test]
    public void Commands_delegate_to_the_host()
    {
        AboutViewModel viewModel = CreateViewModel(["Alice"]);

        viewModel.OpenHomepageCommand.Execute(null);
        viewModel.DonateCommand.Execute(null);
        viewModel.OpenIconsAuthorCommand.Execute(null);
        viewModel.ShowContributorsCommand.Execute(null);
        viewModel.CopyEnvironmentInfoCommand.Execute(null);

        _host.Actions.Should().Equal(
            $"url:{AboutViewModel.HomepageUrl}",
            "url:https://donate.example",
            $"url:{AboutViewModel.IconsAuthorUrl}",
            "contributors",
            "copy");
    }

    private AboutViewModel CreateViewModel(IReadOnlyList<string> contributors, Random? random = null)
        => new(new AboutStrings(), "Gitanas", "Gitanas 1.0", contributors, "https://donate.example", _host, random);

    private sealed class RecordingHost : IAboutDialogHost
    {
        public List<string> Actions { get; } = [];

        public void OpenUrl(string url) => Actions.Add($"url:{url}");

        public void ShowContributors() => Actions.Add("contributors");

        public void CopyEnvironmentInfo() => Actions.Add("copy");
    }
}
