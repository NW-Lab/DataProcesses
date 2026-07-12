using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Core.Tests;

public sealed class FlowModelTests
{
    [Fact]
    public void CanConnect_ReturnsTrue_ForMatchingOutputAndInput()
    {
        var source = new PortDefinition(
            "out",
            "Output",
            PortDirection.Output,
            PortDataKind.FastStream);
        var target = new PortDefinition(
            "in",
            "Input",
            PortDirection.Input,
            PortDataKind.FastStream);

        Assert.True(ConnectionValidator.CanConnect(source, target));
    }

    [Fact]
    public void CanConnect_ReturnsFalse_ForDifferentDataKinds()
    {
        var source = new PortDefinition(
            "out",
            "Output",
            PortDirection.Output,
            PortDataKind.FastStream);
        var target = new PortDefinition(
            "in",
            "Input",
            PortDirection.Input,
            PortDataKind.JsonMessage);

        Assert.False(ConnectionValidator.CanConnect(source, target));
    }

    [Fact]
    public void FastStreamFrame_ReportsChannelAndSampleCounts()
    {
        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["ch1", "ch2"],
            Samples:
            [
                new double[] { 1, 2, 3 }.AsMemory(),
                new double[] { 4, 5, 6 }.AsMemory(),
            ],
            SequenceNumber: 1);

        Assert.Equal(2, frame.ChannelCount);
        Assert.Equal(3, frame.SampleCount);
    }
}