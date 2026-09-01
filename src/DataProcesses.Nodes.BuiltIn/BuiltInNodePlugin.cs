using DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.BreathSt;
using DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodSt;
using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.FftSt;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterSt;
using DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;
using DataProcesses.Nodes.BuiltIn.Blocks.HartRateSt;
using DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;
using DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;
using DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;
using DataProcesses.Nodes.BuiltIn.Blocks.Trigger;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn;

/// <summary>
/// Exposes the set of Blocks distributed with the application.
/// </summary>
public sealed class BuiltInNodePlugin : INodePlugin
{
    private static readonly IReadOnlyCollection<INodeFactory> Factories =
    [
        new CsvInputNodeFactory(),
        new SerialInputStNodeFactory(),
        new SerialInputVectorNodeFactory(),
        new BleInputStNodeFactory(),
        new BleInputVectorNodeFactory(),
        new CameraInputImageNodeFactory(),
        new MovieInputImageNodeFactory(),
        new UVCameraInputImageNodeFactory(),
        new TestSignalNodeFactory(),
        new TestSignalVecNodeFactory(),
        new TestSignalImgNodeFactory(),
        new TriggerNodeFactory(),
        new FilterStNodeFactory(),
        new MovingAverageNodeFactory(),
        new FastFourierTransformNodeFactory(),
        new FftStNodeFactory(),
        new CdTimeResolvedMethodStNodeFactory(),
        new HartRateStNodeFactory(),
        new HartRateImageNodeFactory(),
        new HumansImageNodeFactory(),
        new BreathImageNodeFactory(),
        new BreathStNodeFactory(),
        new StremOutputTSNodeFactory(),
        new StreamOutputVectorNodeFactory(),
        new StreamChartVectorNodeFactory(),
        new StreamChartStNodeFactory(),
        new StreamOutputImageNodeFactory(),
        new PythonOutputNodeFactory(),
        new PayloadOutputNodeFactory(),
        new CsvOutputNodeFactory(),
    ];

    public string Id => "dataprocesses.built-in";

    public string Version => "0.1.0";

    public IReadOnlyCollection<INodeFactory> NodeFactories => Factories;
}

