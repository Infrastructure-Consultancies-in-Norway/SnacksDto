using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLI.Services;

public class GitHubFileDownloader
{
    private const string GitHubRepoOwner = "Infrastructure-Consultancies-in-Norway";
    private const string GitHubRepoName = "Infrastructure-Consultancies-in-Norway.github.io";
    private const string FilePath = "Files/Egenskapsstruktur.xlsx";
    private const string RawContentUrl = $"https://raw.githubusercontent.com/{GitHubRepoOwner}/{GitHubRepoName}/master/{FilePath}";
    private const string ApiUrl = $"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/commits?path={FilePath}&per_page=1";

    private static readonly HttpClient Client = new();

    static GitHubFileDownloader()
    {
        Client.DefaultRequestHeaders.Add("User-Agent", "SnacksDto-CLI");
    }

    /// <summary>
    /// Checks if a newer version exists on GitHub and downloads it if needed.
    /// </summary>
    public async Task<bool> UpdateIfNeededAsync(string localFilePath, bool forceDownload = false)
    {
        try
        {
            if (forceDownload)
            {
                return await PromptAndDownloadAsync(localFilePath, "Force download requested");
            }

            if (!File.Exists(localFilePath))
            {
                return await PromptAndDownloadAsync(localFilePath, "File does not exist locally");
            }

            var localModified = File.GetLastWriteTimeUtc(localFilePath);
            var remoteModified = await GetRemoteFileModificationTimeAsync();

            if (remoteModified == null)
            {
                Console.WriteLine("Could not check GitHub version. Using local file.");
                return false;
            }

            if (remoteModified > localModified)
            {
                return await PromptAndDownloadAsync(localFilePath, $"GitHub version is newer ({remoteModified:yyyy-MM-dd} vs {localModified:yyyy-MM-dd})");
            }

            Console.WriteLine($"Local file is up-to-date (modified: {localModified:yyyy-MM-dd})");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to check for updates: {ex.Message}");
            Console.WriteLine("Proceeding with local file if available.");
            return false;
        }
    }

    /// <summary>
    /// Gets the modification time of the file on GitHub.
    /// </summary>
    private async Task<DateTime?> GetRemoteFileModificationTimeAsync()
    {
        try
        {
            var response = await Client.GetAsync(ApiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                return null;
            }

            var firstCommit = root[0];
            if (firstCommit.TryGetProperty("commit", out var commitObj) &&
                commitObj.TryGetProperty("author", out var author) &&
                author.TryGetProperty("date", out var dateElement))
            {
                if (DateTime.TryParse(dateElement.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var date))
                {
                    return date;
                }
            }

            return null;
        }
        catch (HttpRequestException)
        {
            throw;
        }
    }

    /// <summary>
    /// Prompts the user and downloads the file if confirmed.
    /// </summary>
    private async Task<bool> PromptAndDownloadAsync(string localFilePath, string reason)
    {
        Console.WriteLine($"Reason: {reason}");
        Console.Write("Download from GitHub? (y/n): ");

        var response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y" && response != "yes")
        {
            Console.WriteLine("Skipping download.");
            return false;
        }

        return await DownloadFileAsync(localFilePath);
    }

    /// <summary>
    /// Downloads the file from GitHub to the specified path.
    /// </summary>
    private async Task<bool> DownloadFileAsync(string localFilePath)
    {
        try
        {
            Console.WriteLine($"Downloading from GitHub...");

            var fileBytes = await Client.GetByteArrayAsync(RawContentUrl);

            var directory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(localFilePath, fileBytes);

            Console.WriteLine($"Successfully downloaded to: {localFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to download file: {ex.Message}");
            throw;
        }
    }
}
