using System.Runtime.InteropServices;
using osu.Framework.Extensions;
using Veldrid;

namespace VCollab.Utils.Graphics.Compute;

public sealed class DoubleBufferedAlphaMaskPacker : IDisposable
{
    private const string ComputeShaderFileName = "AlphaPackerShader.hlsl";
    private readonly Type AlphaMaskPackerType = typeof(DoubleBufferedAlphaMaskPacker);

    // Frame count starts at -2 because the first frame will be available on frame 2
    public long FrameCount { get; private set; } = -2;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ResourceFactory _resourceFactory;

    private Shader _computeShader = null!;
    private ResourceLayout _layout = null!;
    private Pipeline _pipeline = null!;

    private Texture? _texture;
    private DeviceBuffer? _outputBuffer;
    private DeviceBuffer? _uniformBuffer;
    private ResourceSet? _resourceSet;

    private readonly BufferedResource<DeviceBuffer>[] _stagingBuffers = [new(), new()];
    private int _gpuStagingIndex = 0;

    private uint _groupsCount;
    private uint _outputSizeInBytes;
    private uint _regionOutputSizeInBytes;

    public DoubleBufferedAlphaMaskPacker(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _resourceFactory = graphicsDevice.ResourceFactory;

        Initialize();
    }

    private void Initialize()
    {
        // Load/compile compute shader
        using var alphaPackerShaderFile =
            AlphaMaskPackerType.Assembly.GetManifestResourceStream(AlphaMaskPackerType, ComputeShaderFileName)!;
        using var shaderSourceTextReader = new StreamReader(alphaPackerShaderFile);

        _computeShader = _resourceFactory.CreateShader(new ShaderDescription(
            ShaderStages.Compute,
            alphaPackerShaderFile.ReadAllBytesToArray(),
            "main"
        ));

        // Pass parameters inside a uniform buffer
        _uniformBuffer = _resourceFactory.CreateBuffer(new BufferDescription(
            (uint) Marshal.SizeOf<TextureSize>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic
        ));

        _layout = _resourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("InputTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("OutputBuffer", ResourceKind.StructuredBufferReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("TextureSize", ResourceKind.UniformBuffer, ShaderStages.Compute)
        ));

        _pipeline = _resourceFactory.CreateComputePipeline(new ComputePipelineDescription(
            _computeShader,
            _layout,
            64, 1, 1
        ));
    }

    public unsafe ReadOnlySpan<byte> ProcessFrame(Texture sourceTexture, TextureRegion textureRegion)
    {
        // Dispatch shader and queue gpu read
        var targetResource = _stagingBuffers[_gpuStagingIndex];
        ref var targetBuffer = ref targetResource.Resource;

        EnsureBufferFormat(sourceTexture, ref targetBuffer, textureRegion);

        var textureSize = new TextureSize(
            (int) textureRegion.OffsetX, (int) textureRegion.OffsetY,
            (int) textureRegion.Width, (int) textureRegion.Height
        );

        // Execute the alpha packing shader and copy output buffer data to the target staging buffer
        using var commands = _resourceFactory.CreateCommandList();
        targetResource.WaitFence?.Dispose();
        targetResource.WaitFence = _resourceFactory.CreateFence(false);

        commands.Begin();

        commands.SetPipeline(_pipeline);
        commands.SetComputeResourceSet(0, _resourceSet);
        commands.UpdateBuffer(_uniformBuffer, 0, textureSize);
        commands.Dispatch(_groupsCount, 1, 1);

        // Copy output to staging buffer
        commands.CopyBuffer(_outputBuffer, 0, targetBuffer, 0, _outputSizeInBytes);

        commands.End();

        _graphicsDevice.SubmitCommands(commands, targetResource.WaitFence);

        // Read previous frame staging texture on CPU if available (should most often be the case)

        // TODO This could be more optimized if it was possible to reduce the device buffers size
        // since it only contains leading 0's but for some reason reducing the buffers size mess up with the packer
        var readbackResource = _stagingBuffers[(_gpuStagingIndex + 1) % _stagingBuffers.Length];

        ReadOnlySpan<byte> data;
        if (readbackResource.Resource is not null && readbackResource.WaitFence?.Signaled is true)
        {
            var readFromTexture = readbackResource.Resource;

            var resource = _graphicsDevice.Map(readFromTexture, MapMode.Read);
            var span = new ReadOnlySpan<byte>(resource.Data.ToPointer(), (int)_regionOutputSizeInBytes);

            _graphicsDevice.Unmap(readFromTexture);

            data = span;
        }
        else
        {
            data = ReadOnlySpan<byte>.Empty;
        }

        SwapBuffers();

        return data;
    }

    private void EnsureBufferFormat(Texture sourceTexture, ref DeviceBuffer? targetBuffer, TextureRegion textureRegion)
    {
        var width = sourceTexture.Width;
        var height = sourceTexture.Height;
        var pixelCount = width * height;

        // Only dispatch for required region size
        var regionPixels = textureRegion.Width * textureRegion.Height;

        // One group is composed of 64 threads and each thread processes 32 pixels
        _groupsCount = (uint)MathF.Ceiling((float)regionPixels / (64 * 32));
        _outputSizeInBytes = (uint)MathF.Ceiling((float)pixelCount / 8);
        _regionOutputSizeInBytes = (uint)MathF.Ceiling((float)regionPixels / 8);

        // Padding to uint size
        var outputBufferSize = _outputSizeInBytes;
        if (outputBufferSize % sizeof(uint) != 0)
        {
            outputBufferSize += sizeof(uint) - (outputBufferSize % sizeof(uint));
        }

        // Also update target texture
        _texture = sourceTexture;

        // Ensure target staging buffer has valid size
        if (targetBuffer is null || targetBuffer.SizeInBytes != _outputSizeInBytes)
        {
            targetBuffer?.Dispose();
            targetBuffer = _resourceFactory.CreateBuffer(new BufferDescription(
                _outputSizeInBytes,
                BufferUsage.Staging
            ));
        }

        // Ensure output buffer for shader has valid size
        if (_outputBuffer is null || _outputBuffer.SizeInBytes != outputBufferSize)
        {
            _outputBuffer?.Dispose();
            _outputBuffer = _resourceFactory.CreateBuffer(new BufferDescription(
                outputBufferSize,
                BufferUsage.StructuredBufferReadWrite,
                sizeof(uint)
            ));

            _resourceSet?.Dispose();
            _resourceSet = _resourceFactory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                _texture, _outputBuffer, _uniformBuffer
            ));
        }
    }

    private void SwapBuffers()
    {
        _gpuStagingIndex = (_gpuStagingIndex + 1) % _stagingBuffers.Length;

        FrameCount++;
    }

    public void Dispose()
    {
        _computeShader.Dispose();
        _layout.Dispose();
        _pipeline.Dispose();
        _outputBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _resourceSet?.Dispose();

        foreach (var bufferedResource in _stagingBuffers)
        {
            bufferedResource.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private record struct TextureSize
    {
        public int OffsetX;
        public int OffsetY;
        public int Width;
        public int Height;

        public TextureSize(int offsetX, int offsetY, int width, int height)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            Width = width;
            Height = height;
        }
    }
}
