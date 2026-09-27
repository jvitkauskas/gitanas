namespace GitCommands.Settings;

/// <summary>Imports the previous application's settings once, leaving the original installation untouched.</summary>
internal static class LegacySettingsMigration
{
    internal static void CopyIfMissing(string legacyDirectory, string destination)
    {
        string source = Path.Join(legacyDirectory, "GitExtensions.settings");
        if (File.Exists(destination) || !File.Exists(source))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        try
        {
            File.Copy(source, destination, overwrite: false);
        }
        catch (IOException) when (File.Exists(destination))
        {
            // Another instance completed the import while this one was starting.
        }
    }
}
