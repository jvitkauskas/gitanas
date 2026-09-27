using GitCommands.Settings;

namespace GitCommandsTests.Settings;

[TestFixture]
public sealed class LegacySettingsMigrationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Import_preserves_the_original_and_never_overwrites_existing_settings(bool alreadyImported)
    {
        string folder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string source = Path.Join(folder, "GitExtensions.settings");
            string destination = Path.Join(folder, "Gitanas", "Gitanas.settings");
            File.WriteAllText(source, "original preferences");
            if (alreadyImported)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, "new preferences");
            }

            LegacySettingsMigration.CopyIfMissing(folder, destination);

            File.ReadAllText(source).Should().Be("original preferences");
            File.ReadAllText(destination).Should().Be(alreadyImported ? "new preferences" : "original preferences");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public void Missing_legacy_settings_do_not_create_an_empty_destination()
    {
        string missing = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string destination = Path.Join(missing, "Gitanas.settings");
        LegacySettingsMigration.CopyIfMissing(missing, destination);
        Directory.Exists(missing).Should().BeFalse();
    }
}
