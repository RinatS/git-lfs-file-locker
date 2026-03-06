using GitLfsFileLocker.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GitLfsFileLocker.VisualStudio.Commands;

internal sealed class ShowLocksCommand
{
    private readonly AsyncPackage _package;

    private ShowLocksCommand(AsyncPackage package, IMenuCommandService commandService)
    {
        _package = package;
        var commandId = new CommandID(CommandSet.Guid, CommandIds.ShowLocks);
        var command = new OleMenuCommand(async (_, _) => await ExecuteAsync(), commandId);
        commandService.AddCommand(command);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
        if (commandService is null)
        {
            return;
        }

        _ = new ShowLocksCommand(package, commandService);
    }

    private async Task ExecuteAsync()
    {
        var window = await _package.ShowToolWindowAsync(typeof(LocksToolWindow), 0, true, _package.DisposalToken);
        if (window?.Frame is null)
        {
            throw new NotSupportedException("Cannot create Git LFS Locks window.");
        }

        await LocksToolWindow.RefreshIfOpenAsync(_package).ConfigureAwait(false);
    }
}
