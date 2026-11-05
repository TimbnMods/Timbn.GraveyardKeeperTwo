// Most of these v3 calls are marked Experimental by Nexus, so expect to adjust them as the API changes.
class NexusClient(string folder)
{
    const string _api = "https://api.nexusmods.com/v3";

    readonly HttpClient _http = new();
    readonly JsonObject _release = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "release.json")))!.AsObject();

    string Text(string key) => _release[key]?.GetValue<string>() ?? throw new ReleaseException($"release.json has no {key}");

    public async Task Run()
    {
        var key = Environment.GetEnvironmentVariable("NEXUS_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            throw new ReleaseException("NEXUS_API_KEY isn't set");
        }
        _http.DefaultRequestHeaders.Add("apikey", key);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("TimbnRelease/1.0");

        var version = Text("version");
        var modId = await GlobalModId(Text("nexusModId"));
        var mainFile = await MainFile(modId);
        if (_release["nexusZip"] == null)
        {
            Console.WriteLine("No zip in this folder, so only the page text is updated");
        }
        else
        {
            // A rerun finds the version already up, and still sets what a failed run may have left out.
            var existing = mainFile?.Versions.FirstOrDefault(entry => entry["version"]?.GetValue<string>() == version);
            if (existing != null)
            {
                Console.WriteLine($"Nexus already has the {version} file");
            }

            var versionId = existing != null ? Id(existing["id"]!) : await UploadFile(modId, mainFile?.Id, version);
            await SetRequirements(versionId);
            await AddChangelog(modId, version);
        }

        var description = File.ReadAllText(Path.Combine(folder, "pages", "nexus.bbcode"));
        await Send(HttpMethod.Patch, $"/mods/{modId}", new JsonObject { ["summary"] = Text("summary"), ["description"] = description });
        Console.WriteLine("Updated the Nexus page text");

        var status = await Status(modId);
        if (status != "published")
        {
            Console.WriteLine($"::warning::The Nexus page is {status}, so the files and text are up but players can't see them. Publish it on the website.");
        }
    }

    async Task<string> GlobalModId(string gameScopedId)
    {
        var mod = await Send(HttpMethod.Get, $"/games/{Repo.NexusGame}/mods/{gameScopedId}");
        return Id(mod["data"]!["id"]!);
    }

    async Task<string?> Status(string modId)
    {
        var batch = await Send(HttpMethod.Post, "/mods/batch", new JsonObject { ["mod_ids"] = new JsonArray(modId) });
        return batch["data"]?["mods"]?.AsArray().FirstOrDefault()?["status"]?.GetValue<string>();
    }

    record ModFile(string Id, List<JsonNode> Versions);

    // The files list carries no category, so the main file is the one whose versions say main.
    async Task<ModFile?> MainFile(string modId)
    {
        var files = await Send(HttpMethod.Get, $"/mods/{modId}/files");
        foreach (var file in files["data"]!["mod_files"]!.AsArray())
        {
            if (file!["is_active"]?.GetValue<bool>() == false)
            {
                continue;
            }

            var id = Id(file["id"]!);
            var versions = (await Send(HttpMethod.Get, $"/mod-files/{id}/versions"))["data"]!["versions"]!.AsArray().Select(entry => entry!).ToList();
            if (versions.Any(entry => entry["category"]?.GetValue<string>() == "main"))
            {
                return new ModFile(id, versions);
            }
        }

        return null;
    }

    async Task<string> UploadFile(string modId, string? mainFileId, string version)
    {
        var zip = Path.Combine(folder, Text("nexusZip"));
        var bytes = await File.ReadAllBytesAsync(zip);
        var name = Path.GetFileName(zip);

        // The single part upload's presigned URL signs a Content-Type nobody documents, so this uses the multipart
        // flow the way Nexus-Mods/upload-action does.
        var upload = (await Send(HttpMethod.Post, "/uploads/multipart", new JsonObject
        {
            ["size_bytes"] = bytes.Length.ToString(),
            ["filename"] = name,
        }))["data"]!;
        var uploadId = Id(upload["id"]!);
        var partSize = upload["part_size_bytes"]!.GetValueKind() == JsonValueKind.String
            ? long.Parse(upload["part_size_bytes"]!.GetValue<string>())
            : upload["part_size_bytes"]!.GetValue<long>();
        var partUrls = upload["part_presigned_urls"]!.AsArray().Select(url => url!.GetValue<string>()).ToList();
        using var storage = new HttpClient();
        var completion = new StringBuilder("<CompleteMultipartUpload>\n");
        for (var part = 0; part < partUrls.Count; part++)
        {
            var offset = (int)(part * partSize);
            var length = (int)Math.Min(partSize, bytes.Length - offset);
            using var put = new HttpRequestMessage(HttpMethod.Put, partUrls[part]) { Content = new ByteArrayContent(bytes, offset, length) };
            put.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using var stored = await storage.SendAsync(put);
            var etag = stored.Headers.ETag?.Tag;
            if (!stored.IsSuccessStatusCode || etag == null)
            {
                throw new ReleaseException($"storing part {part + 1} of {name} failed with {(int)stored.StatusCode} {await stored.Content.ReadAsStringAsync()}");
            }

            completion.Append($"  <Part>\n    <PartNumber>{part + 1}</PartNumber>\n    <ETag>{etag}</ETag>\n  </Part>\n");
        }

        completion.Append("</CompleteMultipartUpload>");
        using (var complete = new HttpRequestMessage(HttpMethod.Post, upload["complete_presigned_url"]!.GetValue<string>()))
        {
            complete.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(completion.ToString()));
            complete.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            using var completed = await storage.SendAsync(complete);
            if (!completed.IsSuccessStatusCode)
            {
                throw new ReleaseException($"completing the upload of {name} failed with {(int)completed.StatusCode} {await completed.Content.ReadAsStringAsync()}");
            }
        }

        await Send(HttpMethod.Post, $"/uploads/{uploadId}/finalise");
        for (var attempt = 0; ; attempt++)
        {
            var state = (await Send(HttpMethod.Get, $"/uploads/{uploadId}"))["data"]!["state"]!.GetValue<string>();
            if (state == "available")
            {
                break;
            }

            if (attempt == 60)
            {
                throw new ReleaseException($"the upload stayed {state} for five minutes");
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        var fields = new JsonObject
        {
            ["upload_id"] = uploadId,
            ["name"] = Text("product"),
            ["version"] = version,
            ["file_category"] = "main",
            ["description"] = Text("nexusFileDescription"),
            ["update_mod_version"] = true,
            ["show_requirements_pop_up"] = true,
        };

        if (mainFileId != null)
        {
            fields["archive_existing_file"] = true;
            var added = await Send(HttpMethod.Post, $"/mod-files/{mainFileId}/versions", fields);
            Console.WriteLine($"Uploaded {name} as a new version of the main file");
            return Id(added["data"]!["version"]!["id"]!);
        }

        fields["mod_id"] = modId;
        await Send(HttpMethod.Post, "/mod-files", fields);
        Console.WriteLine($"Uploaded {name} as the first main file");

        // The id createModFile returns isn't one the mod-files calls accept, so the new file is found from the mod's file list.
        var newFile = await MainFile(modId) ?? throw new ReleaseException("the new main file doesn't show in the mod's file list");
        return Id(newFile.Versions.First(entry => entry["version"]?.GetValue<string>() == version)["id"]!);
    }

    // v3 can only add changelog entries, and adding twice repeats them, so the v1 API says whether the version has one.
    async Task AddChangelog(string modId, string version)
    {
        try
        {
            using var response = await _http.GetAsync($"https://api.nexusmods.com/v1/games/{Repo.NexusGame}/mods/{Text("nexusModId")}/changelogs.json");
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new ReleaseException($"reading the changelog failed with {(int)response.StatusCode} {text}");
            }

            if (JsonNode.Parse(text) is JsonObject logs && logs.ContainsKey(version))
            {
                Console.WriteLine($"Nexus already has the {version} changelog");
                return;
            }

            var changelog = string.Join("\n", _release["changes"]!.AsArray().Select(change => "- " + change!.GetValue<string>()));
            await Send(HttpMethod.Post, $"/mods/{modId}/changelogs", new JsonObject { ["version"] = version, ["changelog"] = changelog });
            Console.WriteLine("Added the Nexus changelog");
        }
        catch (ReleaseException e)
        {
            Console.WriteLine($"::warning::The Nexus changelog wasn't added, so add it on the website. {e.Message}");
        }
    }

    // Requirements are Core at its floor. A failure here only warns, since the file is already up.
    async Task SetRequirements(string versionId)
    {
        try
        {
            var definitions = new JsonArray();
            var floor = _release["coreFloor"]?.GetValue<string>();
            if (floor != null)
            {
                var core = await MainFile(await GlobalModId(Text("coreNexusModId")));
                var atFloor = core?.Versions.FirstOrDefault(entry => entry["version"]?.GetValue<string>() == floor)
                    ?? throw new ReleaseException($"Core's main file on Nexus has no version {floor}. Release Core first.");
                definitions.Add(Range(atFloor));
            }

            await Send(HttpMethod.Put, $"/mod-file-versions/{versionId}/dependencies/ranges", new JsonObject { ["dependency_definitions"] = definitions });
            Console.WriteLine("Set the Nexus requirements");
        }
        catch (ReleaseException e)
        {
            Console.WriteLine($"::warning::Nexus requirements weren't set, so set them on the website. {e.Message}");
        }
    }

    static JsonObject Range(JsonNode version) => new()
    {
        ["ranges"] = new JsonArray(new JsonObject { ["min_version_id"] = Id(version["id"]!), ["max_version_id"] = null }),
    };

    static string Id(JsonNode node) => node.GetValueKind() == JsonValueKind.Number ? node.GetValue<long>().ToString() : node.GetValue<string>();

    async Task<JsonNode> Send(HttpMethod method, string path, JsonObject? body = null)
    {
        using var request = new HttpRequestMessage(method, _api + path);
        if (body != null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new ReleaseException($"Nexus {method} {path} failed with {(int)response.StatusCode} {text}");
        }

        return string.IsNullOrWhiteSpace(text) ? new JsonObject() : JsonNode.Parse(text)!;
    }
}
