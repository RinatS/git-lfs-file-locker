using GitLfsFileLocker.VisualStudio.Services;
using GitLfsFileLocker.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GitLfsFileLocker.VisualStudio.Commands;

internal sealed class UnlockFileCommand : BaseGitLfsCommand
{
    private readonly IMenuCommandService _commandService;

    private UnlockFileCommand(AsyncPackage package, IMenuCommandService commandService) : base(package)
    {
        _commandService = commandService;
        var commandId = new CommandID(CommandSet.Guid, CommandIds.UnlockFile);
        var command = new OleMenuCommand(async (_, _) => await ExecuteAsync(CancellationToken.None), commandId);
        _commandService.AddCommand(command);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
        if (commandService is null)
        {
            return;
        }

        _ = new UnlockFileCommand(package, commandService);
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var files = await GetSelectedFilesAsync();
            if (files.Count == 0)
            {
                await ShowMessageAsync("No file selected. Select one or more files in Solution Explorer.");
                return;
            }

            foreach (var file in files)
            {
                var targetFile = ResolveAlLayoutFileIfNeeded(file);
                await GitLfsService.ExecuteOnFileAsync("unlock", targetFile, cancellationToken).ConfigureAwait(false);
            }

            await ShowMessageAsync($"Unlock requested for {files.Count} file(s).");
            await LocksToolWindow.RefreshIfOpenAsync(Package).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Failed to unlock file(s): {ex.Message}");
        }
    }
}
