using DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.MovingAverage;

public sealed class MovingAverageBlockTests
{
    [Fact]
    public async Task OnPacketAsync_SampleWindowAveragesAcrossFrames()
    {
        var context = new RecordingNodeContext();
        var node = new MovingAverageNode(new MovingAverageSettings(WindowSize: 3));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(MovingAverageBlock.InputPortId, CreateFrame(0, [1.0, 2.0], 0), CancellationToken.None);
        await node.OnPacketAsync(MovingAverageBlock.InputPortId, CreateFrame(2_000_000, [4.0, 8.0], 1), CancellationToken.None);

        var frames = context.EmittedPackets.Select(static packet => Assert.IsType<FastStreamFrame>(packet.Packet)).ToArray();
        Assert.Equal(new[] { 1.0, 1.5 }, frames[0].Samples[0].ToArray());
        Assert.Equal(new[] { 7.0 / 3.0, 14.0 / 3.0 }, frames[1].Samples[0].ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_DurationWindowExpiresOldSamples()
    {
        var context = new RecordingNodeContext();
        var node = new MovingAverageNode(new MovingAverageSettings(MovingAverageWindowMode.Duration, WindowDurationMilliseconds: 2.0));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(MovingAverageBlock.InputPortId, CreateFrame(0, [1.0, 3.0, 9.0, 13.0], 0), CancellationToken.None);

        var emitted = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(new[] { 1.0, 2.0, 13.0 / 3.0, 25.0 / 3.0 }, emitted.Samples[0].ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public void Settings_FromJsonRejectsInvalidWindowSettings()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MovingAverageSettings.FromJson("{\"windowSize\":0}"));
        Assert.Throws<ArgumentException>(() => MovingAverageSettings.FromJson("{\"windowMode\":\"unknown\"}"));
    }

    [Fact]
    public async Task OnPacketAsync_TimeWindowRejectsNonPositiveSamplePeriod()
    {
        var node = new MovingAverageNode(new MovingAverageSettings(MovingAverageWindowMode.Duration));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(MovingAverageBlock.InputPortId, CreateFrame(0, [1.0], 0, 0), CancellationToken.None));
    }

    private static FastStreamFrame CreateFrame(long startTimeNanoseconds, double[] samples, long sequenceNumber, long samplePeriodNanoseconds = 1_000_000)
    {
        return new FastStreamFrame(startTimeNanoseconds, samplePeriodNanoseconds, ["signal"], [samples.AsMemory()], sequenceNumber);
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}