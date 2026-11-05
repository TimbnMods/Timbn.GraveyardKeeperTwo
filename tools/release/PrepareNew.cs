// Gets a mod that has never been released ready for its first release. It is the only command that edits the repo, and
// every step skips itself when it is already done, so a rerun is safe. It never runs on a runner.
// A dry run writes nothing and never calls steamcmd. It prints each step, and a step that a real run would refuse
// prints "would stop" and carries on, so the rest of the run is still checked.
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
            Console.WriteLine("would build Release with -p:PluginDir=Z:/nope");
        }
        else
        {
            Repo.Run("dotnet", repo, "build", csproj, "-c", "Release", "-p:PluginDir=Z:/nope");
        }

        var id = await CreateWorkshopItem(repo, mod, dllDir, dryRun);
        if (dryRun)
        {
            Console.WriteLine($"would write <WorkshopItemId> into {Path.GetFileName(csproj)}");
            return;
        }

        WriteWorkshopItemId(csproj, id);
        Console.WriteLine($"Created the private Workshop item {id} and wrote it to {Path.GetFileName(csproj)}");
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
        File.WriteAllText(csproj, Regex.Replace(text, "<Version>[^<]*</Version>", "<Version>1.0.0</Version>"));
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

    static async Task<string> CreateWorkshopItem(string repo, ModInfo mod, string? dllDir, bool dryRun)
    {
        var user = Environment.GetEnvironmentVariable("STEAM_USERNAME");
        if (string.IsNullOrEmpty(user))
        {
            Refuse(dryRun, "STEAM_USERNAME isn't set. Use the account steamcmd has a cached login for.");
        }

        var steamcmd = dryRun ? TryFindOnPath("steamcmd") ?? "steamcmd" : FindOnPath("steamcmd");
        if (dryRun && TryFindOnPath("steamcmd") == null)
        {
            Console.WriteLine("would stop: steamcmd isn't on PATH");
        }

        var outDir = Path.Combine(Path.GetTempPath(), "timbn-workshop-" + mod.ModName);
        var sha = Repo.Run("git", repo, "rev-parse", "HEAD").Trim();
        var page = PageParser.Load(Path.Combine(mod.DocsDir, "page.yml"));
        // A dry run lets the built DLL's version differ from the csproj, since the version is not bumped yet.
        var options = new Options(mod.ModName, dllDir, outDir, sha, dryRun) { CreateWorkshop = true };
        new ReleaseBuilder(repo, mod, page, options).Run();

        var vdf = Path.Combine(outDir, mod.ProjectName, "workshop", "item.vdf");
        if (dryRun)
        {
            Console.WriteLine($"would run {steamcmd} +login {user ?? "<STEAM_USERNAME>"} +workshop_build_item {vdf} +quit");
            foreach (var line in File.ReadLines(vdf).Where(line => line.Contains("publishedfileid") || line.Contains("visibility") || line.Contains("\"title\"")))
            {
                Console.WriteLine("  item.vdf " + line.Trim());
            }

            return "0";
        }

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(steamcmd)
        {
            ArgumentList = { "+login", user!, "+workshop_build_item", vdf, "+quit" },
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

        // steamcmd writes the new id back into item.vdf. The output is the fallback.
        var written = Regex.Match(File.ReadAllText(vdf), "\"publishedfileid\"\\s+\"(\\d+)\"");
        if (written.Success && written.Groups[1].Value != "0")
        {
            return written.Groups[1].Value;
        }

        var printed = Regex.Match(output, @"(?i)publishedfileid\D{0,20}(\d{6,})");
        return printed.Success ? printed.Groups[1].Value : throw new ReleaseException("steamcmd finished but gave no item id. Find the item on Steam and add WorkshopItemId by hand.");
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

    static string FindOnPath(string name) => TryFindOnPath(name) ?? throw new ReleaseException($"{name} isn't on PATH");

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
