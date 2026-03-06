using GitLfsFileLocker.VisualStudio.Commands;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace GitLfsFileLocker.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("Git LFS File Locker", "Lock and unlock Git LFS files from Visual Studio", "0.1")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(LocksToolWindow), Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Right)]
[Guid(PackageGuidString)]
public sealed class GitLfsFileLockerPackage : AsyncPackage
{
    public const string PackageGuidString = "d598cab7-9f65-40c1-a89c-7207f8f8d2f6";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        await LockFileCommand.InitializeAsync(this);
        await UnlockFileCommand.InitializeAsync(this);
        await ShowLocksCommand.InitializeAsync(this);
    }
}
