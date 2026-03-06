using GitLfsFileLocker.VisualStudio.Services;
using Xunit;

namespace GitLfsFileLocker.VisualStudio.Tests;

public class GitLfsServiceTests
{
    [Fact]
    public void GetGitRelativePath_ReturnsForwardSlashPath()
    {
        var repositoryRoot = @"C:\repo";
        var filePath = @"C:\repo\folder\file.docx";

        var relativePath = GitLfsService.GetGitRelativePath(repositoryRoot, filePath);

        Assert.Equal("folder/file.docx", relativePath);
    }

    [Fact]
    public void ParseLocksJson_ParsesArrayPayload()
    {
        const string json = """
        [
          {
            "id": "123",
            "path": "docs/a.docx",
            "owner": { "name": "alice" },
            "locked_at": "2026-03-06T00:00:00Z"
          }
        ]
        """;

        var locks = GitLfsService.ParseLocksJson(json);

        var lockEntry = Assert.Single(locks);
        Assert.Equal("123", lockEntry.id);
        Assert.Equal("docs/a.docx", lockEntry.path);
        Assert.Equal("alice", lockEntry.owner.name);
    }

    [Fact]
    public void ParseLocksJson_ParsesNestedLocksPayload()
    {
        const string json = """
        {
          "locks": [
            {
              "id": "456",
              "path": "src/layout.docx",
              "owner": { "name": "bob" },
              "locked_at": "2026-03-06T01:00:00Z"
            }
          ]
        }
        """;

        var locks = GitLfsService.ParseLocksJson(json);

        var lockEntry = Assert.Single(locks);
        Assert.Equal("456", lockEntry.id);
        Assert.Equal("bob", lockEntry.owner.name);
    }

    [Fact]
    public void ParseLocksJson_ReturnsEmpty_ForInvalidJson()
    {
        var locks = GitLfsService.ParseLocksJson("{not-valid-json}");
        Assert.Empty(locks);
    }
}
