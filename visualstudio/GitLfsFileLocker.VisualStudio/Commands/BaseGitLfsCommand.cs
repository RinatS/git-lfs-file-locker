using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Task = System.Threading.Tasks.Task;

namespace GitLfsFileLocker.VisualStudio.Commands;

internal abstract class BaseGitLfsCommand
{
    private static readonly Regex LayoutRegex = new Regex("(?:WordLayout|LayoutFile)\\s*=\\s*'([^']+)';", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected readonly AsyncPackage Package;

    protected BaseGitLfsCommand(AsyncPackage package)
    {
        Package = package;
    }

    protected async Task<IReadOnlyList<string>> GetSelectedFilesAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await Package.GetServiceAsync(typeof(DTE)) as DTE2;
        if (dte?.ToolWindows?.SolutionExplorer?.SelectedItems is not Array selectedItems || selectedItems.Length == 0)
        {
            return Array.Empty<string>();
        }

        var files = new List<string>();
        foreach (var selectedItem in selectedItems.OfType<UIHierarchyItem>())
        {
            if (selectedItem.Object is ProjectItem projectItem)
            {
                var file = TryGetFilePath(projectItem);
                if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                {
                    files.Add(file);
                }
            }
        }

        if (files.Count > 0)
        {
            return files.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        var activeDocument = dte?.ActiveDocument?.FullName;
        if (!string.IsNullOrWhiteSpace(activeDocument) && File.Exists(activeDocument))
        {
            return new[] { activeDocument };
        }

        return Array.Empty<string>();
    }

    protected static string? TryGetFilePath(ProjectItem projectItem)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
            {
                return projectItem.FileNames[1];
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    protected async Task ShowMessageAsync(string message, OLEMSGICON icon = OLEMSGICON.OLEMSGICON_INFO)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        VsShellUtilities.ShowMessageBox(
            Package,
            message,
            "Git LFS File Locker",
            icon,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    protected static string ResolveAlLayoutFileIfNeeded(string filePath)
    {
        if (!filePath.EndsWith(".al", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        var fileContent = File.ReadAllText(filePath);
        var match = LayoutRegex.Match(fileContent);
        if (!match.Success)
        {
            return filePath;
        }

        var layoutPath = match.Groups[1].Value;
        var baseDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var combinedPath = Path.GetFullPath(Path.Combine(baseDirectory, layoutPath));
        return File.Exists(combinedPath) ? combinedPath : filePath;
    }

}
