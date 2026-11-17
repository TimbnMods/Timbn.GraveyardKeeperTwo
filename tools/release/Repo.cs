static class Repo
{
    public const string ProjectPrefix = "Timbn.GraveyardKeeperTwo.";
    public const string GitHubRepo = "TimbnMods/Timbn.GraveyardKeeperTwo";
    public const string SteamAppId = "4358690";
    public const string NexusGame = "graveyardkeeper2";
    public const string ThunderstoreCommunity = "graveyard-keeper-2";

    public static string Find()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Timbn.GraveyardKeeperTwo.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new ReleaseException("run this from inside the Timbn.GraveyardKeeperTwo repo");
    }

    public static string Run(string file, string workingDir, params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo(file)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new ReleaseException($"{file} {string.Join(' ', arguments)} failed\n{output.Result}{error.Result}");
        }

        return output.Result;
    }
}
