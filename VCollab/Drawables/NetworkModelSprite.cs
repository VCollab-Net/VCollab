using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using TurboJpegWrapper;
using VCollab.Networking;
using VCollab.Utils;
using VCollab.Utils.Graphics;
using VCollab.Utils.Graphics.Compute;
using Veldrid;
using Image = SixLabors.ImageSharp.Image;
using Texture = Veldrid.Texture;
using OsuTexture = osu.Framework.Graphics.Textures.Texture;

namespace VCollab.Drawables;

public partial class NetworkModelSprite : Sprite, INetworkFrameConsumer
{
    public string? UserName { get; set; } = null;
    public ConcurrentBag<FullFrameData>? FramesBag { get; set; } = null;

    private readonly List<FullFrameData> _availableFrames = new(15);

    private readonly TJDecompressor _jpegDecompressor = new();
    private readonly ArrayBufferWriter<byte> _alphaDecompressBuffer = new();

    private Scheduler _drawThreadScheduler = null!;
    private VeldridRenderer _renderer = null!;
    private GraphicsDevice _graphicsDevice = null!;
    private AlphaMaskUnpacker _alphaUnpacker = null!;
    private bool _isInitialized = false;

    private FullFrameData? _frameToDisplay = null;
    private long _lastDisplayedFrameCount = 0;

    private OsuTexture? _targetTexture = null;
    private bool _needsTextureUpdate = false;

    public NetworkModelSprite()
    {
        // Make sure this sprite always fill its container and keep aspect ratio
        RelativeSizeAxes = Axes.Both;
        FillMode = FillMode.Fill;
    }

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        // Veldrid interop
        if (host.Renderer is not VeldridRenderer renderer || renderer.Device.BackendType != GraphicsBackend.Direct3D11)
        {
            var exception = new NotSupportedException("The spout receiver only works for Veldrid with D3D11 surface");

            Logger.Error(exception, "Unsupported platform and renderer");

            throw exception;
        }

        _renderer = renderer;
        _graphicsDevice = renderer.Device;
        _drawThreadScheduler = host.DrawThread.Scheduler;
        _alphaUnpacker = new AlphaMaskUnpacker(renderer.Device);

        _isInitialized = true;
    }

    protected override void Update()
    {
        if (_needsTextureUpdate)
        {
            _needsTextureUpdate = false;

            Texture?.Dispose();
            Texture = _targetTexture;

            // Required to properly update parent size
            if (Parent is DraggableResizableSprite resizableSprite)
            {
                var width = (float) Texture!.Width;
                var height = (float) Texture!.Height;

                resizableSprite.UpdateSizeFromChild(new Vector2(width, height));
            }
        }

        if (!_isInitialized || UserName is null || FramesBag is null)
        {
            return;
        }

        // Check for available frames and save them to files for debug
        while (!FramesBag.IsEmpty)
        {
            if (!FramesBag.TryTake(out var frameData))
            {
                break;
            }

            // Never display older frames
            if (frameData.FrameCount <= _lastDisplayedFrameCount)
            {
                frameData.Dispose();

                continue;
            }

            _availableFrames.Add(frameData);
        }

        // Always select most recent frame, Update() has higher frequency than Draw() so it works well enough
        FullFrameData? frameToDisplay = null;
        if (_availableFrames.Count > 0)
        {
            frameToDisplay = _availableFrames.MinBy(frame => frame.FrameCount)!;

            var previousFrame = Interlocked.Exchange(ref _frameToDisplay, frameToDisplay);
            previousFrame?.Dispose();

            _drawThreadScheduler.AddOnce(DrawFrame);
            _lastDisplayedFrameCount = frameToDisplay.FrameCount;
        }

        // Reinject frames that we did not use if they are still valid
        foreach (var availableFrame in _availableFrames)
        {
            if (availableFrame != frameToDisplay)
            {
                FramesBag?.Add(availableFrame);
            }
        }

        _availableFrames.Clear();
    }

    private void DrawFrame()
    {
        var frameToDisplay = Interlocked.Exchange(ref _frameToDisplay, null);

        if (frameToDisplay is null)
        {
            return;
        }

        var textureInfo = frameToDisplay.TextureInfo;

        // First frame received, initial texture is always null
        if (_targetTexture is null)
        {
            EnsureTextureFormat(textureInfo, PixelFormat.R8G8B8A8UNorm);
        }

        if (_targetTexture?.NativeTexture is not VeldridNativeTexture veldridTexture)
        {
            return;
        }

        var targetTexture = veldridTexture.VeldridTexture;

        // Check if texture needs re-initialization
        EnsureTextureFormat(textureInfo, targetTexture.Format);

        UpdateTextureData(targetTexture, frameToDisplay.TextureDataSpan, textureInfo);
        UpdateTextureAlpha(targetTexture, frameToDisplay.AlphaDataSpan, frameToDisplay.UncompressedAlphaDataSize);

        // Free up frame associated buffer
        frameToDisplay.Dispose();
    }

    private void UpdateTextureData(Texture targetTexture, ReadOnlySpan<byte> data, TextureInfo textureInfo)
    {
        // Decompress jpeg data
        var rawTextureData = _jpegDecompressor.DecompressShared(
            data,
            (int) textureInfo.Width,
            (int) textureInfo.Height,
            Marshal.SizeOf<Rgba32>(),
            JpegUtils.VeldridToJpegPixelFormat(textureInfo.PixelFormat),
            TJFlags.NONE
        );

        _graphicsDevice.UpdateTexture(
            targetTexture,
            rawTextureData,
            0,
            0,
            0,
            textureInfo.Width,
            textureInfo.Height,
            1,
            0,
            0
        );
    }

    private void UpdateTextureAlpha(Texture targetTexture, ReadOnlySpan<byte> data, int uncompressedDataSize)
    {
        // Decompress alpha data
        _alphaDecompressBuffer.ResetWrittenCount();
        var decompressSpan = _alphaDecompressBuffer.GetSpan(uncompressedDataSize);
        LZ4Codec.Decode(data, decompressSpan);
        _alphaDecompressBuffer.Advance(uncompressedDataSize);

        var alphaData = _alphaDecompressBuffer.WrittenSpan;
        _alphaUnpacker.UnpackAlphaData(targetTexture, alphaData);
    }

    private void EnsureTextureFormat(TextureInfo textureInfo, PixelFormat textureFormat)
    {
        if (_targetTexture is null
            || _targetTexture.Width != textureInfo.Width
            || _targetTexture.Height != textureInfo.Height
            || textureFormat != textureInfo.PixelFormat)
        {
            _targetTexture = _renderer.CreateTexture(new VeldridNativeTexture(
                _renderer,
                textureInfo.Width,
                textureInfo.Height,
                textureInfo.PixelFormat,
                TextureUsage.Sampled | TextureUsage.Storage
            ), WrapMode.ClampToBorder, WrapMode.ClampToBorder);

            _needsTextureUpdate = true;
        }
    }

    private async Task SaveFrameData(FullFrameData frameData)
    {
        var frameCount = frameData.FrameCount;
        var textureInfo = frameData.TextureInfo;
        var textureData = frameData.TextureDataMemory;

        var baseFilePath = Path.Combine("tmp", $"{UserName}-frame-{frameCount:D5}");

        // Decompress alpha data
        var uncompressedAlphaData = ArrayPool<byte>.Shared.Rent(frameData.UncompressedAlphaDataSize);
        var uncompressedAlphaDataSize = LZ4Codec.Decode(frameData.AlphaDataSpan, uncompressedAlphaData);

        // Save alpha later as viewable image
        using var image = Image.LoadPixelData<L8>(uncompressedAlphaData.AsSpan()[..uncompressedAlphaDataSize], (int)textureInfo.Width / 8, (int)textureInfo.Height);
        image.Mutate(i => i.Resize((int)textureInfo.Width, (int)textureInfo.Height, new NearestNeighborResampler()));

        var writeTextureTask = File.WriteAllBytesAsync($"{baseFilePath}.jpg", textureData);
        var alphaWriteTask = image.SaveAsPngAsync($"{baseFilePath}.png");

        await Task.WhenAll(writeTextureTask, alphaWriteTask);

        ArrayPool<byte>.Shared.Return(uncompressedAlphaData);
        frameData.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // This frame consumer is disposed when the peer state becomes invalid, most often due to disconnect
            if (Parent is DraggableResizableSprite resizableSprite)
            {
                Parent.Expire();
            }
            else
            {
                Expire();
            }

            _jpegDecompressor.Dispose();
        }

        base.Dispose(disposing);
    }
}