﻿#if (NETCOREAPP && !NET7_0_OR_GREATER) || (NETFRAMEWORK) || (NETSTANDARD)
#nullable enable
// ReSharper disable RedundantUsingDirective
// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart

using System.Threading;
using System.Threading.Tasks;
namespace System.IO
{
    internal static partial class StreamExt
    {
        // Signature-compatible replacement for ReadAtLeast(Span<byte>, ...)
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readatleast
        public static int ReadAtLeast(
            this Stream stream,
            byte[] buffer,
            int minimumBytes,
            bool throwOnEndOfStream = true
        )
        {
            var totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = stream.Read(
                    buffer,
                    totalBytesRead,
                    Math.Min(minimumBytes, buffer.Length - totalBytesRead)
                );

                if (bytesRead <= 0)
                    break;

                totalBytesRead += bytesRead;
            }

            if (totalBytesRead < minimumBytes && throwOnEndOfStream)
                throw new EndOfStreamException();

            return totalBytesRead;
        }

        // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactly#system-io-stream-readexactly(system-byte()-system-int32-system-int32)
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                var bytesRead = stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead <= 0)
                    throw new EndOfStreamException();

                totalBytesRead += bytesRead;
            }
        }

        // Signature-compatible replacement for ReadExactly(Span<byte>)
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactly#system-io-stream-readexactly(system-span((system-byte)))
        public static void ReadExactly(this Stream stream, byte[] buffer) =>
            stream.ReadExactly(buffer, 0, buffer.Length);

#if FEATURE_TASK
    // Signature-compatible replacement for ReadAtLeastAsync(Memory<byte>, ...)
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readatleastasync
    public static async Task<int> ReadAtLeastAsync(
        this Stream stream,
        byte[] buffer,
        int minimumBytes,
        bool throwOnEndOfStream = true,
        CancellationToken cancellationToken = default
    )
    {
        var totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await stream
                .ReadAsync(
                    buffer,
                    totalBytesRead,
                    Math.Min(minimumBytes, buffer.Length - totalBytesRead),
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (bytesRead <= 0)
                break;

            totalBytesRead += bytesRead;
        }

        if (totalBytesRead < minimumBytes && throwOnEndOfStream)
            throw new EndOfStreamException();

        return totalBytesRead;
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactlyasync#system-io-stream-readexactlyasync(system-byte()-system-int32-system-int32-system-threading-cancellationtoken)
    public static async Task ReadExactlyAsync(
        this Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken = default
    )
    {
        var totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            var bytesRead = await stream
                .ReadAsync(
                    buffer,
                    offset + totalBytesRead,
                    count - totalBytesRead,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (bytesRead <= 0)
                throw new EndOfStreamException();

            totalBytesRead += bytesRead;
        }
    }

    // Signature-compatible replacement for ReadExactlyAsync(Memory<byte>, ...)
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactlyasync#system-io-stream-readexactlyasync(system-memory((system-byte))-system-threading-cancellationtoken)
    public static async Task ReadExactlyAsync(
        this Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken = default
    ) =>
        await stream
            .ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken)
            .ConfigureAwait(false);
#endif

#if FEATURE_MEMORY
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readatleast
    public static int ReadAtLeast(
        this Stream stream,
        Span<byte> buffer,
        int minimumBytes,
        bool throwOnEndOfStream = true
    )
    {
        var totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = stream.Read(buffer.Slice(totalBytesRead));
            if (bytesRead <= 0)
                break;

            totalBytesRead += bytesRead;
        }

        if (totalBytesRead < minimumBytes && throwOnEndOfStream)
            throw new EndOfStreamException();

        return totalBytesRead;
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactly#system-io-stream-readexactly(system-byte()-system-int32-system-int32)
    public static void ReadExactly(this Stream stream, Span<byte> buffer)
    {
        var bufferArray = buffer.ToArray();
        stream.ReadExactly(bufferArray, 0, bufferArray.Length);
        bufferArray.CopyTo(buffer);
    }
#endif

#if FEATURE_TASK && FEATURE_MEMORY
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readatleastasync
    public static async Task<int> ReadAtLeastAsync(
        this Stream stream,
        Memory<byte> buffer,
        int minimumBytes,
        bool throwOnEndOfStream = true,
        CancellationToken cancellationToken = default
    )
    {
        var totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await stream
                .ReadAsync(buffer.Slice(totalBytesRead), cancellationToken)
                .ConfigureAwait(false);

            if (bytesRead <= 0)
                break;

            totalBytesRead += bytesRead;
        }

        if (totalBytesRead < minimumBytes && throwOnEndOfStream)
            throw new EndOfStreamException();

        return totalBytesRead;
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readexactlyasync#system-io-stream-readexactlyasync(system-memory((system-byte))-system-threading-cancellationtoken)
    public static async Task ReadExactlyAsync(
        this Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        var bufferArray = buffer.ToArray();
        await stream
            .ReadExactlyAsync(bufferArray, 0, bufferArray.Length, cancellationToken)
            .ConfigureAwait(false);

        bufferArray.CopyTo(buffer);
    }
#endif
    }
}
#endif