using System.Buffers;

namespace VCollab.Utils.Extensions;

public static class MemoryExtensions
{
    public static ReadOnlySpan<T> WriteToBufferSpan<T>(
        this ReadOnlySpan<T> source,
        ArrayBufferWriter<T> arrayBufferWriter
    )
    {
        arrayBufferWriter.ResetWrittenCount();

        var destination = arrayBufferWriter.GetSpan(source.Length);
        source.CopyTo(destination);

        arrayBufferWriter.Advance(source.Length);

        return arrayBufferWriter.WrittenSpan;
    }

    public static ReadOnlyMemory<T> WriteToBufferMemory<T>(
        this ReadOnlySpan<T> source,
        ArrayBufferWriter<T> arrayBufferWriter
    )
    {
        arrayBufferWriter.ResetWrittenCount();

        var destination = arrayBufferWriter.GetSpan(source.Length);
        source.CopyTo(destination);

        arrayBufferWriter.Advance(source.Length);

        return arrayBufferWriter.WrittenMemory;
    }
}