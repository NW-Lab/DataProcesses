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
    public void CanConnect_ReturnsTrue_WhenEitherSchemaIsUnspecified()
    {
        var source = new PortDefinition(
            "out",
            "Output",
            PortDirection.Output,
            PortDataKind.FastStream,
            DataSchema: PortDataSchema.NumericVector1D);
        var target = new PortDefinition(
            "in",
            "Input",
            PortDirection.Input,
            PortDataKind.FastStream,
            DataSchema: PortDataSchema.Unspecified);

        Assert.True(ConnectionValidator.CanConnect(source, target));
    }

    [Fact]
    public void CanConnect_ReturnsFalse_ForDifferentSpecifiedSchemas()
    {
        var source = new PortDefinition(
            "out",
            "Output",
            PortDirection.Output,
            PortDataKind.FastStream,
            DataSchema: PortDataSchema.Image2D);
        var target = new PortDefinition(
            "in",
            "Input",
            PortDirection.Input,
            PortDataKind.FastStream,
            DataSchema: PortDataSchema.NumericMatrix2D);

        Assert.False(ConnectionValidator.CanConnect(source, target));
    }

    [Fact]
    public void CanConnect_ReturnsTrue_ForMatchingSpecifiedSchemas()
    {
        var source = new PortDefinition(
            "out",
            "Output",
            PortDirection.Output,
            PortDataKind.FastStream,
            DataSchema: PortDataSchema.Image2D);
        var target = new PortDefinition(
            "in",
            "Input",
            PortDirection.Input,
            PortDataKind.FastStream,
            DataSchema: PortDataSchema.Image2D);

        Assert.True(ConnectionValidator.CanConnect(source, target));
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
    public void NumericVectorFrame_ReportsLength()
    {
        var frame = new NumericVectorFrame(
            Name: "fft-magnitude",
            Values: new double[] { 1, 2, 3, 4 }.AsMemory(),
            SequenceNumber: 7,
            Timestamp: DateTimeOffset.UnixEpoch);

        Assert.Equal(4, frame.Length);
        Assert.Equal(PortDataKind.FastStream, frame.Kind);
    }

    [Fact]
    public void NumericMatrixFrame_Throws_WhenShapeDoesNotMatchBufferLength()
    {
        var values = new double[] { 1, 2, 3 }.AsMemory();

        var exception = Assert.Throws<ArgumentException>(() =>
            new NumericMatrixFrame("matrix", rowCount: 2, columnCount: 2, values, sequenceNumber: 1));

        Assert.Contains("Values length", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ImageFrame_UsesInterleavedHwcBuffer()
    {
        var pixels = new byte[]
        {
            255, 0, 0,
            0, 255, 0,
        }.AsMemory();

        var frame = new ImageFrame(
            name: "preview",
            width: 2,
            height: 1,
            pixelFormat: ImagePixelFormat.Rgb24,
            pixelsInterleaved: pixels,
            sequenceNumber: 2,
            timestamp: DateTimeOffset.UnixEpoch);

        Assert.Equal(3, frame.ChannelCount);
        Assert.Equal(PortDataKind.FastStream, frame.Kind);
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

    [Fact]
    public void Validate_ReturnsInvalid_WhenConnectionSchemaConflictsWithSourcePortSchema()
    {
        var sourceDefinition = SourceDefinition with
        {
            Ports =
            [
                new PortDefinition(
                    "out",
                    "Output",
                    PortDirection.Output,
                    PortDataKind.FastStream,
                    DataSchema: PortDataSchema.NumericVector1D),
            ],
        };

        var sinkDefinition = SinkDefinition with
        {
            Ports =
            [
                new PortDefinition(
                    "in",
                    "Input",
                    PortDirection.Input,
                    PortDataKind.FastStream,
                    DataSchema: PortDataSchema.NumericVector1D),
            ],
        };

        var document = new FlowDocument(
            Guid.NewGuid(),
            "Mismatched connection schema",
            [
                new NodeInstance("source-1", sourceDefinition.TypeId, 0, 0, "{}"),
                new NodeInstance("sink-1", sinkDefinition.TypeId, 100, 0, "{}"),
            ],
            [
                new Connection(
                    "source-1",
                    "out",
                    "sink-1",
                    "in",
                    PortDataKind.FastStream,
                    DataSchema: PortDataSchema.Image2D),
            ]);

        var result = FlowValidator.Validate(document, [sourceDefinition, sinkDefinition]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == FlowValidationIssueCode.IncompatiblePorts);
    }
}