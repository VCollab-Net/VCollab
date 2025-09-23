using System.Runtime.InteropServices;
using osu.Framework.Extensions;
using Veldrid;

namespace VCollab.Utils.Graphics.Compute;

public sealed class AlphaMaskUnpacker : IDisposable
{
    private const string ComputeShaderFileName = "AlphaUnpackerShader.hlsl";
    private readonly Type AlphaMaskUnpackerType = typeof(AlphaMaskUnpacker);

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ResourceFactory _resourceFactory;

    private Shader _computeShader = null!;
    private ResourceLayout _layout = null!;
    private Pipeline _pipeline = null!;

    private DeviceBuffer? _inputBuffer;
    private DeviceBuffer? _stagingBuffer;
    private DeviceBuffer? _uniformBuffer;
    private ResourceSet? _resourceSet;

    private uint _groupsCount;

    public AlphaMaskUnpacker(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _resourceFactory = graphicsDevice.ResourceFactory;

        Initialize();
    }

    private void Initialize()
    {
        // Load/compile compute shader
        using var alphaUnpackerShaderFile =
            AlphaMaskUnpackerType.Assembly.GetManifestResourceStream(AlphaMaskUnpackerType, ComputeShaderFileName)!;
        using var shaderSourceTextReader = new StreamReader(alphaUnpackerShaderFile);

        _computeShader = _resourceFactory.CreateShader(new ShaderDescription(
            ShaderStages.Compute,
            alphaUnpackerShaderFile.ReadAllBytesToArray(),
            "main"
        ));

        // Pass parameters inside a uniform buffer
        _uniformBuffer = _resourceFactory.CreateBuffer(new BufferDescription(
            (uint) Marshal.SizeOf<TextureSize>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic
        ));

        _layout = _resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("OutputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("InputBuffer", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TextureSize", ResourceKind.UniformBuffer, ShaderStages.Compute)
        ));

        _pipeline = _resourceFactory.CreateComputePipeline(new ComputePipelineDescription(
            _computeShader,
            _layout,
            64, 1, 1
        ));
    }

    public unsafe void UnpackAlphaData(Texture targetTexture, ReadOnlySpan<byte> alphaData)
    {
        // Dispatch shader and queue gpu read
        EnsureBufferFormat(targetTexture);

        var textureSize = new TextureSize(
            (int) targetTexture.Width, (int) targetTexture.Height
        );

        // Prepare input data into staging buffer
        var mappedStagingBuffer = _graphicsDevice.Map(_stagingBuffer, MapMode.Write);

        // The mapped staging buffer is not directly resized, we need to wait for the next frame if it happens
        if (mappedStagingBuffer.SizeInBytes < alphaData.Length)
        {
            _graphicsDevice.Unmap(_stagingBuffer);

            return;
        }

        var stagingData = new Span<byte>(mappedStagingBuffer.Data.ToPointer(), (int) mappedStagingBuffer.SizeInBytes);
        alphaData.CopyTo(stagingData);

        _graphicsDevice.Unmap(_stagingBuffer);

        // Execute the alpha unpacking shader, this will directly write into the texture
        using var commands = _resourceFactory.CreateCommandList();

        commands.Begin();

        commands.SetPipeline(_pipeline);
        commands.SetComputeResourceSet(0, _resourceSet);
        commands.UpdateBuffer(_uniformBuffer, 0, textureSize);
        commands.CopyBuffer(_stagingBuffer, 0, _inputBuffer, 0, _stagingBuffer!.SizeInBytes);
        commands.Dispatch(_groupsCount, 1, 1);

        commands.End();

        _graphicsDevice.SubmitCommands(commands);
    }

    private void EnsureBufferFormat(Texture targetTexture)
    {
        var width = targetTexture.Width;
        var height = targetTexture.Height;
        var pixelCount = width * height;

        // One group is composed of 64 threads and each thread processes 32 pixels
        _groupsCount = (uint)MathF.Ceiling((float)pixelCount / (64 * 32));
        var inputSizeInBytes = (uint)MathF.Ceiling((float)pixelCount / 8);

        // Padding to uint size
        var inputBufferSize = inputSizeInBytes;
        if (inputBufferSize % sizeof(uint) != 0)
        {
            inputBufferSize += sizeof(uint) - (inputBufferSize % sizeof(uint));
        }

        // Ensure target staging buffer has valid size
        if (_inputBuffer is null || _inputBuffer.SizeInBytes != inputBufferSize)
        {
            _inputBuffer?.Dispose();
            _inputBuffer = _resourceFactory.CreateBuffer(new BufferDescription(
                inputBufferSize,
                BufferUsage.StructuredBufferReadOnly,
                sizeof(uint)
            ));

            _stagingBuffer?.Dispose();
            _stagingBuffer = _resourceFactory.CreateBuffer(new BufferDescription(
                inputBufferSize,
                BufferUsage.Staging
            ));

            _resourceSet?.Dispose();
            _resourceSet = _resourceFactory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                targetTexture, _inputBuffer, _uniformBuffer
            ));
        }
    }

    public void Dispose()
    {
        _computeShader.Dispose();
        _layout.Dispose();
        _pipeline.Dispose();
        _inputBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _resourceSet?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private record struct TextureSize
    {
        public int Width;
        public int Height;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private long _padding;

        public TextureSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
