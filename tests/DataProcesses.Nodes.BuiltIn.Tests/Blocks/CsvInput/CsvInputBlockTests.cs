using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.CsvInput;

public sealed class CsvInputBlockTests
{
    [Fact]
    public void BuiltInCatalog_RegistersCsvInputBlock()
    {
        var plugin = new BuiltInNodePlugin();

        var factory = Assert.Single(
            plugin.NodeFactories,
            factory => string.Equals(factory.Definition.TypeId, CsvInputBlock.TypeId, StringComparison.Ordinal));

        Assert.Equal(CsvInputBlock.TypeId, factory.Definition.TypeId);
        Assert.Equal("CSV Input", factory.Definition.DisplayName);
        Assert.Equal(NodeType.Input, factory.Definition.NodeType);
        Assert.Equal("CsvInput", factory.Definition.Title);
        Assert.Equal("File/COM CSV", factory.Definition.Subtitle);
    }

    [Fact]
    public void CsvInputBlock_DefinesFastStreamOutputsOnly()
    {
        Assert.Equal(CsvInputBlock.MaxStreamOutputs, CsvInputBlock.Definition.Ports.Count);
        Assert.All(
            CsvInputBlock.Definition.Ports,
            port =>
            {
                Assert.Equal(PortDirection.Output, port.Direction);
                Assert.Equal(PortDataKind.FastStream, port.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
                Assert.False(port.IsRequired);
            });

        Assert.Equal("stream-1", CsvInputBlock.Definition.Ports[0].Id);
        Assert.Equal($"stream-{CsvInputBlock.MaxStreamOutputs}", CsvInputBlock.Definition.Ports[^1].Id);
    }

    [Fact]
    public async Task StartAsync_EmitsMillisAndValueFramesPerOutputPort()
    {
        var lines = new[]
        {
            "millis,CH1Value,CH2Value",
            "0,1.5,2.5",
            "10,1.7,2.7",
        };

        var settings = CsvInputSettings.Default with
        {
            OutputCount = 2,
            SourceType = CsvInputSourceType.File,
            FilePath = "dummy.csv",
            FilePlaybackMode = CsvFilePlaybackMode.Immediate,
            HasHeaderRow = true,
        };

        var context = new RecordingNodeContext();
        var node = new CsvInputNode(
            settings,
            getTimestamp: () => DateTimeOffset.UnixEpoch,
            lineSourceFactory: (_, _) => CreateLineSource(lines));

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Equal(4, context.EmittedPackets.Count);

        var firstPortFirstPacket = Assert.IsType<FastStreamFrame>(context.EmittedPackets[0].Packet);
        Assert.Equal("stream-1", context.EmittedPackets[0].OutputPortId);
        Assert.Equal(["millis", "value"], firstPortFirstPacket.ChannelNames);
        Assert.Equal(0, firstPortFirstPacket.Samples[0].Span[0]);
        Assert.Equal(1.5, firstPortFirstPacket.Samples[1].Span[0]);

        var secondPortFirstPacket = Assert.IsType<FastStreamFrame>(context.EmittedPackets[1].Packet);
        Assert.Equal("stream-2", context.EmittedPackets[1].OutputPortId);
        Assert.Equal(0, secondPortFirstPacket.Samples[0].Span[0]);
        Assert.Equal(2.5, secondPortFirstPacket.Samples[1].Span[0]);

        var firstPortSecondPacket = Assert.IsType<FastStreamFrame>(context.EmittedPackets[2].Packet);
        Assert.Equal(10, firstPortSecondPacket.Samples[0].Span[0]);
        Assert.Equal(1.7, firstPortSecondPacket.Samples[1].Span[0]);
        Assert.Equal(1, firstPortSecondPacket.SequenceNumber);

        var secondPortSecondPacket = Assert.IsType<FastStreamFrame>(context.EmittedPackets[3].Packet);
        Assert.Equal(10, secondPortSecondPacket.Samples[0].Span[0]);
        Assert.Equal(2.7, secondPortSecondPacket.Samples[1].Span[0]);
        Assert.Equal(1, secondPortSecondPacket.SequenceNumber);
    }

    [Fact]
    public async Task StartAsync_FileMillisPlayback_WaitsByMillisDelta()
    {
        var lines = new[]
        {
            "100,1",
            "130,2",
            "125,3",
        };

        var capturedDelays = new List<TimeSpan>();

        var settings = CsvInputSettings.Default with
        {
            OutputCount = 1,
            SourceType = CsvInputSourceType.File,
            FilePath = "dummy.csv",
            FilePlaybackMode = CsvFilePlaybackMode.Millis,
            HasHeaderRow = false,
        };

        var context = new RecordingNodeContext();
        var node = new CsvInputNode(
            settings,
            delayAsync: (duration, _) =>
            {
                capturedDelays.Add(duration);
                return Task.CompletedTask;
            },
            lineSourceFactory: (_, _) => CreateLineSource(lines));

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        var delay = Assert.Single(capturedDelays);
        Assert.Equal(TimeSpan.FromMilliseconds(30), delay);
    }

    [Fact]
    public async Task StartAsync_ThrowsWhenCsvRowDoesNotContainAllConfiguredChannels()
    {
        var lines = new[]
        {
            "0,1",
        };

        var settings = CsvInputSettings.Default with
        {
            OutputCount = 2,
            SourceType = CsvInputSourceType.File,
            FilePath = "dummy.csv",
            HasHeaderRow = false,
        };

        var node = new CsvInputNode(settings, lineSourceFactory: (_, _) => CreateLineSource(lines));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await node.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void Factory_CreatesConfiguredNodeFromSettingsJson()
    {
        var factory = new CsvInputNodeFactory();

        var node = factory.CreateNode(
            "csv-input-1",
            "{\"outputCount\":3,\"sourceType\":\"file\",\"filePath\":\"sample.csv\",\"filePlaybackMode\":\"millis\",\"hasHeaderRow\":true}");

        Assert.IsType<CsvInputNode>(node);
    }

    private static async IAsyncEnumerable<string> CreateLineSource(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            yield return line;
            await Task.Yield();
        }
    }
}
