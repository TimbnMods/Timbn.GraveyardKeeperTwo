static class Changelog
{
    public static List<string> Read(string path, string version)
    {
        if (!File.Exists(path))
        {
            throw new ReleaseException($"no changelog at {path}");
        }

        return TryRead(path, version) ?? throw new ReleaseException($"{Path.GetFileName(path)} has no bullets under ## {version}");
    }

    public static List<string>? TryRead(string path, string version)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var changes = new List<string>();
        var inVersion = false;
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("## "))
            {
                if (inVersion)
                {
                    break;
                }

                inVersion = line[3..].Trim() == version;
            }
            else if (inVersion && line.StartsWith("- "))
            {
                changes.Add(line[2..].Trim());
            }
            else if (inVersion && changes.Count > 0 && line.StartsWith("  ") && line.Trim() != "")
            {
                changes[^1] += " " + line.Trim();
            }
        }

        return changes.Count > 0 ? changes : null;
    }
}
