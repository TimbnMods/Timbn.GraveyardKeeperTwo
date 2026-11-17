// Gets a mod that has never been released ready for its first release. It is the only command that edits the repo, and
// every step skips itself when it is already done, so a rerun is safe. It never runs on a runner.
// A dry run writes nothing and never calls steamcmd. It prints each step, and a step that a real run would refuse
// prints "would stop" and carries on, so the rest of the run is still checked.
record SteamLogin(string User, string SteamCmd);

static class PrepareNew
{
    public static async Task Run(string repo, string name, string? dllDir, bool skipWorkshop, bool dryRun)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            Refuse(dryRun, "prepare-new only runs on a local machine, since it creates a Workshop item with your Steam login");
        }

        var mod = ModInfo.Load(repo, name);
        if (ModTags.Read(repo, mod).Count > 0)
        {
            Refuse(dryRun, $"{mod.ProjectName} already has a release tag, so it is not new");
        }

        var login = new SteamLogin(string.Empty, string.Empty);
        if (!skipWorkshop && !mod.ToWorkshop)
        {
            login = ResolveSteam(dryRun);
        }

        var csproj = Path.Combine(mod.ProjectDir, mod.ProjectName + ".csproj");
        SetVersion(csproj, mod, dryRun);
        RenameChangelogHeading(mod, dryRun);
        if (!dryRun)
        {
            mod = ModInfo.Load(repo, name);
        }

        if (skipWorkshop)
        {
            Console.WriteLine("Skipped the Workshop item");
            return;
        }

        if (mod.ToWorkshop)
        {
            Refuse(dryRun, $"WorkshopItemId is already {mod.WorkshopItemId}, so no item is created");
            if (!dryRun)
            {
                return;
            }
        }

        if (dryRun)
        {
            Console.WriteLine("would build Release with -p:DeployTo=none");
        }
        else
        {
            Repo.Run("dotnet", repo, "build", csproj, "-c", "Release", "-p:DeployTo=none");
        }

        var id = await CreateWorkshopItem(repo, mod, dllDir, login, dryRun);
        if (dryRun)
        {
            Console.WriteLine($"would write <WorkshopItemId> into {Path.GetFileName(csproj)}");
            return;
        }

        WriteWorkshopItemId(csproj, id);
        Console.WriteLine($"Created the private Workshop item {id} and wrote it to {Path.GetFileName(csproj)}");
    }

    static SteamLogin ResolveSteam(bool dryRun)
    {
        var user = Environment.GetEnvironmentVariable("STEAM_USERNAME") ?? string.Empty;
        if (string.IsNullOrEmpty(user))
        {
            Refuse(dryRun, "STEAM_USERNAME isn't set. Use the account steamcmd has a cached login for.");
        }

        var steamcmd = TryFindOnPath("steamcmd") ?? string.Empty;
        if (string.IsNullOrEmpty(steamcmd))
        {
            Refuse(dryRun, "steamcmd isn't on PATH");
        }

        return new SteamLogin(user, steamcmd);
    }

    static void Refuse(bool dryRun, string message)
    {
        if (!dryRun)
        {
            throw new ReleaseException(message);
        }

        Console.WriteLine($"would stop: {message}");
    }

    static void SetVersion(string csproj, ModInfo mod, bool dryRun)
    {
        if (System.Version.Parse(mod.Version) >= new System.Version(1, 0, 0))
        {
            return;
        }

        if (dryRun)
        {
            Console.WriteLine($"would set the version from {mod.Version} to 1.0.0");
            return;
        }

        var text = File.ReadAllText(csproj);
        if (Regex.IsMatch(text, "<Version>[^<]*</Version>"))
        {
            text = Regex.Replace(text, "<Version>[^<]*</Version>", "<Version>1.0.0</Version>");
        }
        else
        {
            // The props default is 0.1.0, so a new mod may carry no Version of its own.
            text = Regex.Replace(text, @"^([ \t]*)(<Product>[^<]*</Product>)(\r?\n)", "$1$2$3$1<Version>1.0.0</Version>$3", RegexOptions.Multiline);
        }

        File.WriteAllText(csproj, text);
        Console.WriteLine($"Set the version from {mod.Version} to 1.0.0");
    }

    static void RenameChangelogHeading(ModInfo mod, bool dryRun)
    {
        var path = Path.Combine(mod.ProjectDir, "CHANGELOG.md");
        if (Changelog.TryRead(path, "1.0.0") != null)
        {
            return;
        }

        if (Changelog.TryRead(path, mod.Version) == null)
        {
            Refuse(dryRun, $"CHANGELOG.md has no bullets under ## {mod.Version} to rename to ## 1.0.0");
            return;
        }

        if (dryRun)
        {
            Console.WriteLine($"would rename the changelog heading ## {mod.Version} to ## 1.0.0");
            return;
        }

        var text = File.ReadAllText(path);
        File.WriteAllText(path, Regex.Replace(text, $@"^## {Regex.Escape(mod.Version)}[ \t]*$", "## 1.0.0", RegexOptions.Multiline));
        Console.WriteLine($"Renamed the changelog heading ## {mod.Version} to ## 1.0.0");
    }

    static async Task<string> CreateWorkshopItem(string repo, ModInfo mod, string? dllDir, SteamLogin login, bool dryRun)
    {
        var outDir = Path.Combine(Path.GetTempPath(), "timbn-workshop-" + mod.ModName);
        var sha = Repo.Run("git", repo, "rev-parse", "HEAD").Trim();
        var page = PageParser.Load(Path.Combine(mod.DocsDir, "page.yml"));
        var options = new Options(mod.ModName, dllDir, outDir, sha, dryRun)
        {
            CreateWorkshop = true,
            PagesOnly = dryRun,
        };

        new ReleaseBuilder(repo, mod, page, options).Run();

        var vdf = Path.Combine(outDir, mod.ProjectName, "workshop", "item.vdf");
        if (dryRun)
        {
            var steamcmd = string.IsNullOrEmpty(login.SteamCmd) ? "steamcmd" : login.SteamCmd;
            var user = string.IsNullOrEmpty(login.User) ? "<STEAM_USERNAME>" : login.User;
            Console.WriteLine($"would run {steamcmd} +login {user} +workshop_build_item {vdf} +quit");
            foreach (var line in File.ReadLines(vdf).Where(line => line.Contains("publishedfileid") || line.Contains("visibility") || line.Contains("\"title\"")))
            {
                Console.WriteLine("  item.vdf " + line.Trim());
            }

            Directory.Delete(outDir, true);
            return "0";
        }

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(login.SteamCmd)
        {
            ArgumentList = { "+login", login.User, "+workshop_build_item", vdf, "+quit" },
            RedirectStandardOutput = true,
            UseShellExecute = false,
        })!;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        Console.WriteLine(output);
        if (process.ExitCode != 0)
        {
            throw new ReleaseException($"steamcmd exited with {process.ExitCode}");
        }

        // steamcmd prints harmless "failed" lines while it starts, so only its own ERROR! lines count.
        if (output.Contains("ERROR!") || !output.Contains("Success"))
        {
            throw new ReleaseException("steamcmd didn't report a successful Workshop upload");
        }

        // steamcmd writes the new id back into item.vdf. The output is the fallback.
        var written = Regex.Match(File.ReadAllText(vdf), "\"publishedfileid\"\\s+\"(\\d+)\"");
        var id = written.Success && written.Groups[1].Value != "0" ? written.Groups[1].Value : string.Empty;
        if (string.IsNullOrEmpty(id))
        {
            var printed = Regex.Match(output, @"(?i)publishedfileid\D{0,20}(\d{6,})");
            id = printed.Success ? printed.Groups[1].Value : throw new ReleaseException("steamcmd finished but gave no item id. Find the item on Steam and add WorkshopItemId by hand.");
        }

        // A failed upload keeps the folder above for debugging, so only a finished one clears it.
        Directory.Delete(outDir, true);
        return id;
    }

    static void WriteWorkshopItemId(string csproj, string id)
    {
        var text = File.ReadAllText(csproj);
        if (Regex.IsMatch(text, "<WorkshopItemId>[^<]*</WorkshopItemId>"))
        {
            text = Regex.Replace(text, "<WorkshopItemId>[^<]*</WorkshopItemId>", $"<WorkshopItemId>{id}</WorkshopItemId>");
        }
        else
        {
            text = Regex.Replace(text, @"(<Version>[^<]*</Version>)(\r?\n)", $"$1$2    <WorkshopItemId>{id}</WorkshopItemId>$2");
        }

        File.WriteAllText(csproj, text);
    }

    static string? TryFindOnPath(string name)
    {
        var extensions = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", "" } : [""];
        // STEAMCMD_DIR is for a steamcmd folder that is not on PATH.
        var folders = (Environment.GetEnvironmentVariable("STEAMCMD_DIR") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH")).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var folder in folders)
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(folder, name + extension);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
