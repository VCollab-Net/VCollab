#pragma kernel main

// Output texture: write to 32 pixels per input
RWTexture2D<unorm float4> OutputTexture : register(u0);

// Input buffer: one uint per group
StructuredBuffer<uint> InputBuffer : register(t0);

// Texture size
cbuffer TextureSize : register(b0)
{
    int Width;
    int Height;
}

[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint threadIndex = id.x;

    // Read input value
    uint inputValue = InputBuffer[threadIndex];

    // Compute starting pixel index
    uint totalPixels = (uint)(Width * Height);
    uint startPixelIndex = threadIndex * 32u;

    // Make sure we don't write out of bounds
    if (startPixelIndex >= totalPixels)
        return;

    // Only process required pixels, useful to process last pixels if texture dimensions are not a multiple of 32
    uint pixelsToProcess = min(32u, totalPixels - startPixelIndex);

    // Write to 32 pixels
    [unroll]
    for (uint i = 0; i < pixelsToProcess; i++)
    {
        uint pixelIndex = startPixelIndex + i;
        int2 pixelCoord = int2(
            (int)(pixelIndex % (uint)Width),
            (int)(pixelIndex / (uint)Width)
        );

        // Read current pixel color
        float4 currentColor = OutputTexture[pixelCoord];

        // Unpack alpha value
        float newAlpha = (float)(inputValue >> i & 1u);

        // Write back with updated alpha
        OutputTexture[pixelCoord] = float4(currentColor.rgb, newAlpha);
    }
}