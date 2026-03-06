using GitLfsFileLocker.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GitLfsFileLocker.VisualStudio;

public partial class LocksControl : UserControl
{
    public event EventHandler? RefreshRequested;
    public event EventHandler<string>? UnlockRequested;

    public LocksControl()
    {
        InitializeComponent();
    }

    public async Task SetStatusAsync(string status)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        StatusText.Text = status;
    }

    internal async Task SetLocksAsync(IReadOnlyList<GitLfsService.LockEntry> locks)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        LocksGrid.ItemsSource = locks;
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnUnlockClicked(object sender, RoutedEventArgs e)
    {
        if (LocksGrid.SelectedItem is GitLfsService.LockEntry selected)
        {
            UnlockRequested?.Invoke(this, selected.id);
        }
    }
}
