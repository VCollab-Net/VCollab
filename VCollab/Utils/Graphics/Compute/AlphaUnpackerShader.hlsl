#pragma kernel main

typedef struct texture_size
{
    int width;
    int height;
} texture_size_t;

// Input buffer: one uint per group
StructuredBuffer<uint> InputBuffer;

// Output texture: write to 32 pixels per input
RWTexture2D<float4> OutputTexture;

// Texture size
texture_size_t TextureSize;

[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint threadIndex = id.x;

    // Read input value
    uint inputValue = InputBuffer[threadIndex];

    // Compute starting pixel index
    uint totalPixels = TextureSize.width * TextureSize.height;
    uint startPixelIndex = threadIndex * 32;

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
        int2 pixelCoord = int2(pixelIndex % TextureSize.width, pixelIndex / TextureSize.width);

        // Read current pixel color
        float4 currentColor = OutputTexture[pixelCoord];

        // Unpack alpha value
        float newAlpha = inputValue >> i & 1;

        // Write back with updated alpha
        OutputTexture[pixelCoord] = float4(currentColor.rgb, newAlpha);
    }
}