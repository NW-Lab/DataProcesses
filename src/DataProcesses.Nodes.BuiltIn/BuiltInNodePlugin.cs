using DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;
using DataProcesses.Nodes.BuiltIn.Blocks.BreathTS;
using DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.FftTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FftFS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;
using DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;
using DataProcesses.Nodes.BuiltIn.Blocks.HartRateTS;
using DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;
using DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;
using DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;
using DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;
using DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartTS;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;
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
        new SerialInputTSNodeFactory(),
        new SerialInputVectorNodeFactory(),
        new BleInputTSNodeFactory(),
        new BleInputFSNodeFactory(),
        new BleInputVectorNodeFactory(),
        new CameraInputImageNodeFactory(),
        new MovieInputImageNodeFactory(),
        new UVCameraInputImageNodeFactory(),
        new MicInputFSNodeFactory(),
        new AudioOutFSNodeFactory(),
            new MqttInNodeFactory(),
            new MqttOutNodeFactory(),
        new TestSignalNodeFactory(),
        new TestSignalFSNodeFactory(),
        new TestSignalVecNodeFactory(),
        new TestSignalImgNodeFactory(),
        new TriggerNodeFactory(),
        new FilterTSNodeFactory(),
        new FilterFSNodeFactory(),
        new MovingAverageNodeFactory(),
        new FastFourierTransformNodeFactory(),
        new FftFSNodeFactory(),
        new FftTSNodeFactory(),
        new CdTimeResolvedMethodTSNodeFactory(),
        new HartRateTSNodeFactory(),
        new HartRateImageNodeFactory(),
        new HumansImageNodeFactory(),
        new BreathImageNodeFactory(),
        new BreathTSNodeFactory(),
        new StremOutputTSNodeFactory(),
        new StreamOutputFSNodeFactory(),
        new StreamOutputVectorNodeFactory(),
        new StreamChartVectorNodeFactory(),
        new StreamChartTSNodeFactory(),
        new StreamChartFSNodeFactory(),
        new StreamOutputImageNodeFactory(),
        new PythonOutputNodeFactory(),
        new PayloadOutputNodeFactory(),
        new CsvOutputNodeFactory(),
    ];

    public string Id => "dataprocesses.built-in";

    public string Version => "0.1.0";

    public IReadOnlyCollection<INodeFactory> NodeFactories => Factories;
}
