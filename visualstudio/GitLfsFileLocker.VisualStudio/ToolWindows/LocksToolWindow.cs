using GitLfsFileLocker.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace GitLfsFileLocker.VisualStudio;

public class LocksToolWindow : ToolWindowPane
{
    private readonly LocksControl _control;

    public LocksToolWindow() : base(null)
    {
        Caption = "Git LFS Locks";
        _control = new LocksControl();
        Content = _control;
        _control.RefreshRequested += async (_, _) => await RefreshAsync(CancellationToken.None).ConfigureAwait(false);
        _control.UnlockRequested += async (_, lockId) => await UnlockAsync(lockId, CancellationToken.None).ConfigureAwait(false);
    }

    public static async Task RefreshIfOpenAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var toolWindow = await package.FindToolWindowAsync(typeof(LocksToolWindow), 0, false, package.DisposalToken).ConfigureAwait(true) as LocksToolWindow;
        if (toolWindow is not null)
        {
            await toolWindow.RefreshAsync(package.DisposalToken).ConfigureAwait(false);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var workingDirectory = await ResolveWorkingDirectoryAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                await _control.SetStatusAsync("Open a solution folder first.").ConfigureAwait(false);
                await _control.SetLocksAsync(Array.Empty<GitLfsService.LockEntry>()).ConfigureAwait(false);
                return;
            }

            var locks = await GitLfsService.GetLocksAsync(workingDirectory, cancellationToken).ConfigureAwait(false);
            await _control.SetStatusAsync($"Active Git LFS locks: {locks.Count}").ConfigureAwait(false);
            await _control.SetLocksAsync(locks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _control.SetStatusAsync($"Failed to load locks: {ex.Message}").ConfigureAwait(false);
            await _control.SetLocksAsync(Array.Empty<GitLfsService.LockEntry>()).ConfigureAwait(false);
        }
    }

    private async Task UnlockAsync(string lockId, CancellationToken cancellationToken)
    {
        try
        {
            var workingDirectory = await ResolveWorkingDirectoryAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                await _control.SetStatusAsync("Open a solution folder first.").ConfigureAwait(false);
                return;
            }

            await GitLfsService.UnlockByIdAsync(lockId, workingDirectory, cancellationToken).ConfigureAwait(false);
            await _control.SetStatusAsync($"Unlocked lock ID {lockId}.").ConfigureAwait(false);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _control.SetStatusAsync($"Failed to unlock lock ID {lockId}: {ex.Message}").ConfigureAwait(false);
        }
    }

    private async Task<string?> ResolveWorkingDirectoryAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = GetService(typeof(DTE)) as DTE;
        var solutionPath = dte?.Solution?.FullName;
        if (!string.IsNullOrWhiteSpace(solutionPath))
        {
            return Path.GetDirectoryName(solutionPath);
        }

        var activeDocumentPath = dte?.ActiveDocument?.FullName;
        if (!string.IsNullOrWhiteSpace(activeDocumentPath))
        {
            return Path.GetDirectoryName(activeDocumentPath);
        }

        return null;
    }
}
