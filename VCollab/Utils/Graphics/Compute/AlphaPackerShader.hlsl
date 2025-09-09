#pragma kernel main

// Input texture
Texture2D<float4> InputTexture;

// Output buffer: one entry per group of 32 pixels
RWStructuredBuffer<uint> OutputBuffer;

// Texture size (passed from C#)
int2 TextureSize;

[numthreads(64, 1, 1)] // One thread processes 32 pixels
void main(uint3 id : SV_DispatchThreadID)
{
    uint threadIndex = id.x;

    // Compute starting pixel index for this thread
    uint totalPixels = TextureSize.x * TextureSize.y;
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
        int2 pixelCoord = int2(pixelIndex % TextureSize.x, pixelIndex / TextureSize.x);

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
