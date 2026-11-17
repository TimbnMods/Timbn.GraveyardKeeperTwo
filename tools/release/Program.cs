using System.CommandLine;

var mod = new Argument<string>("mod")
{
    Description = "The project name with or without the Timbn.GraveyardKeeperTwo. prefix, in any case (chestlabels, Core).",
};

var dll = new Option<string?>("--dll")
{
    Description = "The folder holding the built DLL, bin/Release of the project by default.",
};

var output = new Option<string?>("--out")
{
    Description = "Where everything is written, artifacts/release by default.",
};

var sha = new Option<string?>("--sha")
{
    Description = "Pins image links to a commit. A real run needs it, and a dry run uses HEAD.",
};

var dryRun = new Option<bool>("--dry-run")
{
    Description = "Also unpacks every zip next to it and lets a DLL version mismatch through with a warning.",
};

var build = new RootCommand("Builds one mod's release packages and page text from its csproj, docs/<Mod>/page.yml and CHANGELOG.md.")
{
    mod,
    dll,
    output,
    sha,
    dryRun,
};

build.SetAction(result => RunSync(() => Release(result, pagesOnly: false)));

var pages = new Command("pages", "Writes only the page text, release.json and a text only item.vdf, with no DLL needed.")
{
    mod,
    output,
    sha,
};

pages.SetAction(result => RunSync(() => Release(result, pagesOnly: true)));
build.Subcommands.Add(pages);

var list = new Command("list", "Prints, as JSON, every project at 1.0.0 or above whose CHANGELOG.md has a section for its current version.");
list.SetAction(_ => RunSync(() => Commands.List(Repo.Find())));
build.Subcommands.Add(list);

var notes = new Command("notes", "Prints the GitHub release body for a project's current version.")
{
    mod,
};

notes.SetAction(result => RunSync(() => Commands.Notes(Repo.Find(), result.GetValue(mod)!)));
build.Subcommands.Add(notes);

var folder = new Argument<string>("folder")
{
    Description = "A release folder from the build or pages command.",
};

var nexus = new Command("nexus", "Uploads a built release folder to its Nexus page, with the key in NEXUS_API_KEY. A folder from pages has no zip, so only the page text is updated.")
{
    folder,
};

nexus.SetAction(result => Run(() => new NexusSite().Upload(result.GetValue(folder)!)));
build.Subcommands.Add(nexus);

var check = new Command("check", "Read only readiness check for one mod. It lists every failure and exits non zero on any.")
{
    mod,
};

check.SetAction(result => Run(async () => Environment.ExitCode = await Check.Run(Repo.Find(), result.GetValue(mod)!)));
build.Subcommands.Add(check);

var skipWorkshop = new Option<bool>("--skip-workshop")
{
    Description = "Leave the Workshop item alone.",
};

var prepareNew = new Command("prepare-new", "Gets a never released mod ready for its first release. Sets 1.0.0, renames the changelog heading and creates a private Workshop item. Local only.")
{
    mod,
    dll,
    skipWorkshop,
    dryRun,
};

prepareNew.SetAction(result => Run(() => PrepareNew.Run(Repo.Find(), result.GetValue(mod)!, result.GetValue(dll), result.GetValue(skipWorkshop), result.GetValue(dryRun))));
build.Subcommands.Add(prepareNew);

var exit = await build.Parse(args).InvokeAsync();
return exit != 0 ? exit : Environment.ExitCode;

void Release(ParseResult result, bool pagesOnly)
{
    var repo = Repo.Find();
    var options = new Options(result.GetValue(mod)!, result.GetValue(dll), result.GetValue(output), result.GetValue(sha), result.GetValue(dryRun))
    {
        PagesOnly = pagesOnly,
    };

    var info = ModInfo.Load(repo, options.Mod);
    var page = PageParser.Load(Path.Combine(info.DocsDir, "page.yml"));
    new ReleaseBuilder(repo, info, page, options).Run();
}

static async Task<int> Run(Func<Task> work)
{
    try
    {
        await work();
        return 0;
    }
    catch (ReleaseException e)
    {
        Console.Error.WriteLine($"error: {e.Message}");
        return 1;
    }
}

static Task<int> RunSync(Action work) => Run(() =>
{
    work();
    return Task.CompletedTask;
});
