using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.CsvOutput;

public sealed class CsvOutputBlockTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "DataProcesses.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Definition_UsesOneFastStreamInputAndNoOutputPorts()
    {
        var port = Assert.Single(CsvOutputBlock.Definition.Ports);

        Assert.Equal(CsvOutputBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(NodeType.Output, CsvOutputBlock.Definition.NodeType);
    }

    [Fact]
    public async Task StartAsync_WritesHeaderAndSpanRowsUsingLatestValues()
    {
        Directory.CreateDirectory(workingDirectory);
        var outputPath = Path.Combine(workingDirectory, "output.csv");
        var epoch = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamps = new Queue<DateTimeOffset>(
        [
            epoch,
            epoch.AddMilliseconds(150),
            epoch.AddMilliseconds(260),
        ]);

        var settings = CsvOutputSettings.Default with
        {
            FilePath = outputPath,
            SpanMilliseconds = 100,
            ExecutionSessionId = 10,
            InputBindings =
            [
                new CsvOutputInputBinding("source-1", "stream-1", "Tag1"),
                new CsvOutputInputBinding("source-2", "stream-1", "Tag2"),
                new CsvOutputInputBinding("source-3", "stream-1", "Tag3"),
            ],
        };

        var node = new CsvOutputNode("csv-output-node", settings, () => timestamps.Dequeue());
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await node.StartAsync(CancellationToken.None);
        await node.OnPacketAsync(
            CsvOutputBlock.InputPortId,
            CreateFrame(1.5),
            "source-1",
            "stream-1",
            "Tag1",
            CancellationToken.None);
        await node.OnPacketAsync(
            CsvOutputBlock.InputPortId,
            CreateFrame(2.5),
            "source-2",
            "stream-1",
            "Tag2",
            CancellationToken.None);
        await node.OnPacketAsync(
            CsvOutputBlock.InputPortId,
            CreateFrame(3.5),
            "source-3",
            "stream-1",
            "Tag3",
            CancellationToken.None);
        await node.StartAsync(CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
        Assert.Equal("#millis,Tag1,Tag2,Tag3", lines[0]);
        Assert.Equal("0,0,0,0", lines[1]);
        Assert.Equal("100,1.5,2.5,3.5", lines[2]);
        Assert.Equal("200,1.5,2.5,3.5", lines[3]);
    }

    [Fact]
    public async Task StartAsync_NewWriteMode_RecreatesFileOnNewExecutionSession()
    {
        Directory.CreateDirectory(workingDirectory);
        var outputPath = Path.Combine(workingDirectory, "recreated.csv");
        await File.WriteAllTextAsync(outputPath, "legacy", CancellationToken.None);

        var epoch = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session1Node = new CsvOutputNode(
            "csv-output-node-new-mode",
            CsvOutputSettings.Default with
            {
                FilePath = outputPath,
                WriteMode = CsvOutputWriteMode.NewFile,
                SpanMilliseconds = 100,
                ExecutionSessionId = 1,
            },
            () => epoch);
        await session1Node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        await session1Node.StartAsync(CancellationToken.None);

        var session2Node = new CsvOutputNode(
            "csv-output-node-new-mode",
            CsvOutputSettings.Default with
            {
                FilePath = outputPath,
                WriteMode = CsvOutputWriteMode.NewFile,
                SpanMilliseconds = 100,
                ExecutionSessionId = 2,
            },
            () => epoch);
        await session2Node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        await session2Node.StartAsync(CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(outputPath, CancellationToken.None);
        Assert.Equal("#millis", lines[0]);
        Assert.Equal("0", lines[1]);
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public void Settings_FromJson_ParsesInputBindingsAndDefaults()
    {
        var settings = CsvOutputSettings.FromJson(
            JsonSerializer.Serialize(new
            {
                filePath = "C:/tmp/out.csv",
                writeMode = "append",
                spanMilliseconds = 120,
                executionSessionId = 7,
                inputBindings = new[]
                {
                    new { sourceNodeId = "source-1", sourcePortId = "stream-1", tag = "A" },
                },
            }));

        Assert.Equal("C:/tmp/out.csv", settings.FilePath);
        Assert.Equal(CsvOutputWriteMode.Append, settings.WriteMode);
        Assert.Equal(120, settings.SpanMilliseconds);
        Assert.Equal(7, settings.ExecutionSessionId);
        var binding = Assert.Single(settings.InputBindings);
        Assert.Equal("source-1", binding.SourceNodeId);
        Assert.Equal("stream-1", binding.SourcePortId);
        Assert.Equal("A", binding.Tag);
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    private static FastStreamFrame CreateFrame(double value)
    {
        return new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["value"],
            Samples: [new[] { value }.AsMemory()],
            SequenceNumber: 1);
    }
}
