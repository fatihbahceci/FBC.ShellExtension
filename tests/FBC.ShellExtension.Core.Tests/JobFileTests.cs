using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Menu;
using static FBC.ShellExtension.Core.Tests.TestItems;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class JobFileTests
{
    private string _folder = "";

    [TestInitialize]
    public void CreateFolder() =>
        _folder = Directory.CreateTempSubdirectory("fbctools-jobs-").FullName;

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

    [TestMethod]
    public void WrittenJob_ReadsBackAndIsDeleted()
    {
        var job = new RunJob
        {
            Label = "Convert",
            Command = new CommandDefinition { FileName = "conv.exe", Arguments = "\"{path}\"", MultiSelect = MultiSelectMode.AllAtOnce },
            Items = [File(@"C:\in\a.txt"), Drive(@"D:\")],
            ShellExtVersion = "0.2.0+abc",
        };

        string path = JobFile.Write(job, _folder);
        RunJob copy = JobFile.ReadAndDelete(path);

        Assert.IsFalse(System.IO.File.Exists(path));
        Assert.AreEqual("Convert", copy.Label);
        Assert.AreEqual(MultiSelectMode.AllAtOnce, copy.Command.MultiSelect);
        CollectionAssert.AreEqual(job.Items, copy.Items);
        Assert.AreEqual("0.2.0+abc", copy.ShellExtVersion);
    }

    [TestMethod]
    public void ItemsCarryOnlyPathAndKind()
    {
        string path = JobFile.Write(new RunJob { Items = [File(@"C:\in\a.txt")] }, _folder);

        string json = System.IO.File.ReadAllText(path);

        Assert.Contains("""{"path":"C:\\in\\a.txt","kind":"file"}""", json);
    }

    [TestMethod]
    [DataRow("""{ "version": 2, "label": "x", "command": { "fileName": "a" }, "items": [] }""", DisplayName = "other version")]
    [DataRow("""{ "label": "x", "items": [ { "path": "C:\\a" } ] }""", DisplayName = "item without kind")]
    [DataRow("not json", DisplayName = "not json")]
    [DataRow("null", DisplayName = "null")]
    public void InvalidJob_IsRejectedAndStillDeleted(string json)
    {
        string path = Path.Combine(_folder, "bad.json");
        System.IO.File.WriteAllText(path, json);

        Assert.ThrowsExactly<JobFileException>(() => JobFile.ReadAndDelete(path));
        Assert.IsFalse(System.IO.File.Exists(path));
    }

    [TestMethod]
    public void MissingJob_IsReported()
    {
        Assert.ThrowsExactly<JobFileException>(() => JobFile.ReadAndDelete(Path.Combine(_folder, "none.json")));
    }

    [TestMethod]
    public void For_CopiesTheClickedCommandAndStampsTheVersion()
    {
        var command = new CommandDefinition { FileName = "tool.exe" };
        var node = new CommandNode("Convert", command, [File(@"C:\in\a.txt")]);

        RunJob job = RunJob.For(node);

        Assert.AreEqual("Convert", job.Label);
        Assert.AreSame(command, job.Command);
        Assert.AreEqual(@"C:\in\a.txt", job.Items.Single().Path);
        Assert.AreEqual(ProductInfo.InformationalVersion, job.ShellExtVersion);
    }
}
