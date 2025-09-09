#pragma kernel main

typedef struct texture_size
{
    int offsetX;
    int offsetY;
    int width;
    int height;
} texture_size_t;

// Input texture
Texture2D<float4> InputTexture;

// Output buffer: one entry per group of 32 pixels
RWStructuredBuffer<uint> OutputBuffer;

// Texture size (passed from C#)
texture_size_t TextureSize;

[numthreads(64, 1, 1)] // One thread processes 32 pixels
void main(uint3 id : SV_DispatchThreadID)
{
    uint threadIndex = id.x;

    // Compute starting pixel index for this thread
    uint totalPixels = TextureSize.width * TextureSize.height;
    uint startPixelIndex = threadIndex * 32;

    // Make sure we don't read out of bounds
    if (startPixelIndex >= totalPixels)
        return;

    // Only process required pixels, useful to process last pixels if texture dimensions are not a multiple of 32
    uint pixelsToProcess = min(32u, totalPixels - startPixelIndex);

    // Read and process 32 pixels
    uint result = 0;

    [unroll]
    for (uint i = 0; i < pixelsToProcess; i++)
    {
        uint pixelIndex = startPixelIndex + i;
        int2 pixelCoord = int2(
            pixelIndex % TextureSize.width + TextureSize.offsetX,
            pixelIndex / TextureSize.width + TextureSize.offsetY
        );

        float4 pixelValue = InputTexture.Load(int3(pixelCoord, 0));
        float alpha = pixelValue.a;

        if (alpha > 0)
        {
            result |= 1 << i;
        }
    }

    // Write result to output buffer
    OutputBuffer[threadIndex] = result;
}
