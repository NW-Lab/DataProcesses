using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Core.Tests;

public sealed class FlowModelTests
{
    private static readonly NodeDefinition SourceDefinition = new(
        "source",
        "Source",
        "Sources",
        "0.1.0",
        [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)]);

    private static readonly NodeDefinition ProcessorDefinition = new(
        "processor",
        "Processor",
        "Processing",
        "0.1.0",
        [
            new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream),
            new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream),
        ]);

    private static readonly NodeDefinition SinkDefinition = new(
        "sink",
        "Sink",
        "Visualization",
        "0.1.0",
        [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream)]);

    private static readonly NodeDefinition OptionalSinkDefinition = new(
        "optional-sink",
        "Optional Sink",
        "Visualization",
        "0.1.0",
        [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream, IsRequired: false)]);

    private static readonly IReadOnlyList<NodeDefinition> Definitions =
    [
        SourceDefinition,
        ProcessorDefinition,
        SinkDefinition,
        OptionalSinkDefinition,
    ];

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

    [Fact]
    public void Validate_ReturnsValid_ForConnectedAcyclicFlow()
    {
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Valid flow",
            [
                new NodeInstance("source-1", SourceDefinition.TypeId, 0, 0, "{}"),
                new NodeInstance("processor-1", ProcessorDefinition.TypeId, 100, 0, "{}"),
                new NodeInstance("sink-1", SinkDefinition.TypeId, 200, 0, "{}"),
            ],
            [
                new Connection("source-1", "out", "processor-1", "in", PortDataKind.FastStream),
                new Connection("processor-1", "out", "sink-1", "in", PortDataKind.FastStream),
            ]);

        var result = FlowValidator.Validate(document, Definitions);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ReturnsInvalid_ForMissingRequiredInput()
    {
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Missing input",
            [new NodeInstance("sink-1", SinkDefinition.TypeId, 0, 0, "{}")],
            []);

        var result = FlowValidator.Validate(document, Definitions);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == FlowValidationIssueCode.MissingRequiredInput);
    }

    [Fact]
    public void Validate_AllowsUnconnectedOptionalInput()
    {
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Optional input",
            [new NodeInstance("sink-1", OptionalSinkDefinition.TypeId, 0, 0, "{}")],
            []);

        var result = FlowValidator.Validate(document, Definitions);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AllowsMissingRequiredInputOnDisabledNode()
    {
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Disabled sink",
            [new NodeInstance("sink-1", SinkDefinition.TypeId, 0, 0, "{}", IsEnabled: false)],
            []);

        var result = FlowValidator.Validate(document, Definitions);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsInvalid_ForUnknownPort()
    {
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Unknown port",
            [
                new NodeInstance("source-1", SourceDefinition.TypeId, 0, 0, "{}"),
                new NodeInstance("sink-1", SinkDefinition.TypeId, 100, 0, "{}"),
            ],
            [new Connection("source-1", "missing", "sink-1", "in", PortDataKind.FastStream)]);

        var result = FlowValidator.Validate(document, Definitions);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == FlowValidationIssueCode.UnknownSourcePort);
    }

    [Fact]
    public void Validate_ReturnsInvalid_ForCycle()
    {
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Cycle",
            [
                new NodeInstance("processor-1", ProcessorDefinition.TypeId, 0, 0, "{}"),
                new NodeInstance("processor-2", ProcessorDefinition.TypeId, 100, 0, "{}"),
            ],
            [
                new Connection("processor-1", "out", "processor-2", "in", PortDataKind.FastStream),
                new Connection("processor-2", "out", "processor-1", "in", PortDataKind.FastStream),
            ]);

        var result = FlowValidator.Validate(document, Definitions);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == FlowValidationIssueCode.CycleDetected);
    }
}