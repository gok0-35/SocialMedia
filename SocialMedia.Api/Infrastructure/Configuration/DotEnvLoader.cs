namespace SocialMedia.Api.Infrastructure.Configuration;

public static class DotEnvLoader
{
    public static void Load(params string[] candidatePaths)
    {
        foreach (string path in candidatePaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            LoadFile(path);
            return;
        }
    }

    private static void LoadFile(string path)
    {
        string[] lines = File.ReadAllLines(path);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].Trim();
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            value = Unquote(value);

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        char first = value[0];
        char last = value[^1];

        if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
        {
            return value[1..^1];
        }

        return value;
    }
}
