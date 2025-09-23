#pragma kernel main

// Input texture
Texture2D<unorm float4> InputTexture : register(t0);

// Output buffer: one entry per group of 32 pixels
RWStructuredBuffer<uint> OutputBuffer : register(u0);

// Texture size
cbuffer TextureSize : register(b0)
{
    int OffsetX;
    int OffsetY;
    int Width;
    int Height;
}

[numthreads(64, 1, 1)] // One thread processes 32 pixels
void main(uint3 id : SV_DispatchThreadID)
{
    uint threadIndex = id.x;

    // Compute starting pixel index for this thread
    uint totalPixels = (uint)(Width * Height);
    uint startPixelIndex = threadIndex * 32u;

    // Make sure we don't read out of bounds
    if (startPixelIndex >= totalPixels)
    {
        return;
    }

    // Only process required pixels, useful to process last pixels if texture dimensions are not a multiple of 32
    uint pixelsToProcess = min(32u, totalPixels - startPixelIndex);

    // Read and process 32 pixels
    uint result = 0u;

    [unroll]
    for (uint i = 0u; i < pixelsToProcess; i++)
    {
        uint pixelIndex = startPixelIndex + i;
        int2 pixelCoord = int2(
            (int)(pixelIndex % (uint)Width) + OffsetX,
            (int)(pixelIndex / (uint)Width) + OffsetY
        );

        float4 pixelValue = InputTexture.Load(int3(pixelCoord, 0));
        float alpha = pixelValue.a;

        if (alpha >= 0.5f)
        {
            result |= 1u << i;
        }
    }

    // Write result to output buffer
    OutputBuffer[threadIndex] = result;
}
