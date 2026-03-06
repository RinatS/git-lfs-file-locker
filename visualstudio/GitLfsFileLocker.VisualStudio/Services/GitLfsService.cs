using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GitLfsFileLocker.VisualStudio.Services;

internal static class GitLfsService
{
    private static readonly Regex LockIdRegex = new Regex("^[A-Za-z0-9-]+$", RegexOptions.Compiled);

    public sealed class LockOwner
    {
        public string name { get; set; } = string.Empty;
    }

    public sealed class LockEntry
    {
        public string id { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
        public LockOwner owner { get; set; } = new();
        public string locked_at { get; set; } = string.Empty;
    }

    public static async Task<string> ExecuteOnFileAsync(string command, string filePath, CancellationToken cancellationToken)
    {
        var repositoryRoot = await GetRepositoryRootAsync(Path.GetDirectoryName(filePath)!, cancellationToken).ConfigureAwait(false);
        var relativePath = GetGitRelativePath(repositoryRoot, filePath);
        return await ExecuteAsync($"lfs {command} \"{relativePath}\"", repositoryRoot, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<LockEntry>> GetLocksAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        var repositoryRoot = await GetRepositoryRootAsync(workingDirectory, cancellationToken).ConfigureAwait(false);
        var output = await ExecuteAsync("lfs locks --json", repositoryRoot, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(output))
        {
            return Array.Empty<LockEntry>();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<LockEntry>>(output);
            if (list is not null)
            {
                return list;
            }

            using var doc = JsonDocument.Parse(output);
            if (doc.RootElement.TryGetProperty("locks", out var locksElement) && locksElement.ValueKind == JsonValueKind.Array)
            {
                var nested = JsonSerializer.Deserialize<List<LockEntry>>(locksElement.GetRawText());
                return nested ?? new List<LockEntry>();
            }
        }
        catch (JsonException)
        {
            // fall through below and return empty with explicit message to caller
        }

        return Array.Empty<LockEntry>();
    }

    public static async Task UnlockByIdAsync(string lockId, string workingDirectory, CancellationToken cancellationToken)
    {
        if (!LockIdRegex.IsMatch(lockId))
        {
            throw new InvalidOperationException("Invalid lock id.");
        }

        var repositoryRoot = await GetRepositoryRootAsync(workingDirectory, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync($"lfs unlock --id=\"{lockId}\"", repositoryRoot, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<string> GetRepositoryRootAsync(string workingDirectory, CancellationToken cancellationToken)
    {
        var output = await ExecuteAsync("rev-parse --show-toplevel", workingDirectory, cancellationToken).ConfigureAwait(false);
        return output.Trim();
    }

    public static string GetGitRelativePath(string repositoryRoot, string filePath)
    {
        var baseUri = new Uri(AppendDirectorySeparator(repositoryRoot), UriKind.Absolute);
        var fileUri = new Uri(filePath, UriKind.Absolute);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString()).Replace('\\', '/');
    }

    private static async Task<string> ExecuteAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await Task.Run(() => process.WaitForExit(), cancellationToken).ConfigureAwait(false);

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(standardError)
                ? $"git {arguments} failed with exit code {process.ExitCode}."
                : standardError.Trim();
            throw new InvalidOperationException(message);
        }

        return standardOutput;
    }

    private static string AppendDirectorySeparator(string path)
    {
        if (path.Length > 0 && path[path.Length - 1] == Path.DirectorySeparatorChar)
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }
}
