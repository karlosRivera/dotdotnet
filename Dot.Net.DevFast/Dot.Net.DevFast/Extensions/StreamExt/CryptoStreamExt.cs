﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dot.Net.DevFast.Etc;

namespace Dot.Net.DevFast.Extensions.StreamExt
{
    /// <summary>
    /// Extensions on Cypto stream for data transformation.
    /// </summary>
    public static class CryptoStreamExt
    {
        /// <summary>
        /// Reads from <paramref name="input"/> and writes transformed data on <paramref name="streamToWrite"/>,
        /// using <paramref name="transform"/>, while observing <paramref name="token"/>.
        /// </summary>
        /// <param name="input">Bytes to transform</param>
        /// <param name="transform">transform to use</param>
        /// <param name="streamToWrite">Stream to write transformed data to.</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="disposeOutput">If true, disposes <paramref name="streamToWrite"/> upon operation completion, else leaves it open</param>
        public static async Task TransformAsync(this byte[] input, ICryptoTransform transform,
            Stream streamToWrite, CancellationToken token, bool disposeOutput = false)
        {
            using (var outputWrapper = new WrappedStream(streamToWrite, disposeOutput))
            {
                using (var transformer = new CryptoStream(outputWrapper, transform, CryptoStreamMode.Write))
                {
                    await transformer.WriteAsync(input, 0, input.Length, token).ConfigureAwait(false);
                    await transformer.FlushAsync(token).ConfigureAwait(false);
                    await outputWrapper.FlushAsync(token).ConfigureAwait(false);
                    await streamToWrite.FlushAsync(token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Reads from <paramref name="streamToRead"/> and writes transformed data on <paramref name="streamToWrite"/>,
        /// using <paramref name="transform"/>, while observing <paramref name="token"/>.
        /// </summary>
        /// <param name="streamToRead">Stream to read from</param>
        /// <param name="transform">transform to use</param>
        /// <param name="streamToWrite">Stream to write transformed data to.</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="disposeInput">If true, disposes <paramref name="streamToRead"/> upon operation completion, else leaves it open</param>
        /// <param name="disposeOutput">If true, disposes <paramref name="streamToWrite"/> upon operation completion, else leaves it open</param>
        /// <param name="copyBufferSize">Buffer size for stream copy operation</param>
        public static async Task TransformAsync(this Stream streamToRead, ICryptoTransform transform,
            Stream streamToWrite, CancellationToken token, bool disposeInput = false,
            bool disposeOutput = false, int copyBufferSize = StdLookUps.DefaultBufferSize)
        {
            using (var outputWrapper = new WrappedStream(streamToWrite, disposeOutput))
            {
                using (var transformer = new CryptoStream(outputWrapper, transform, CryptoStreamMode.Write))
                {
                    using (var inputWrapper = new WrappedStream(streamToRead, disposeInput))
                    {
                        await inputWrapper.CopyToAsync(transformer, copyBufferSize, token).ConfigureAwait(false);
                        await transformer.FlushAsync(token).ConfigureAwait(false);
                        await outputWrapper.FlushAsync(token).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Reads characters from <paramref name="input"/> and writes transformed data on <paramref name="streamToWrite"/>,
        /// using <paramref name="transform"/> and <paramref name="encoding"/> while observing 
        /// <paramref name="token"/> for cancellation.
        /// </summary>
        /// <param name="input">String to convert</param>
        /// <param name="transform">transform to use</param>
        /// <param name="streamToWrite">Stream to write transformed data to.</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="disposeOutput">If true, disposes <paramref name="streamToWrite"/> upon operation completion, else leaves it open</param>
        /// <param name="bufferSize">Buffer size for character reading</param>
        /// <param name="encoding">Encoding to use to get string bytes, if not supplied UTF8 is used</param>
        public static Task TransformAsync(this StringBuilder input, ICryptoTransform transform,
            Stream streamToWrite, CancellationToken token, bool disposeOutput = false,
            int bufferSize = StdLookUps.DefaultBufferSize, Encoding encoding = null)
        {
            return TransformChunks(streamToWrite, transform, input.Length, encoding ?? Encoding.UTF8,
                token, disposeOutput, bufferSize, input.CopyTo);
        }

        /// <summary>
        /// Reads characters from <paramref name="input"/> and writes transformed data on <paramref name="streamToWrite"/>, 
        /// using <paramref name="transform"/> and <paramref name="encoding"/> while observing <paramref name="token"/> for cancellation.
        /// </summary>
        /// <param name="input">String to convert</param>
        /// <param name="transform">transform to use</param>
        /// <param name="streamToWrite">Stream to write transformed data to.</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="disposeOutput">If true, disposes <paramref name="streamToWrite"/> upon operation completion, else leaves it open</param>
        /// <param name="bufferSize">Buffer size for character reading</param>
        /// <param name="encoding">Encoding to use to get string bytes, if not supplied UTF8 is used</param>
        public static Task TransformAsync(this string input, ICryptoTransform transform,
            Stream streamToWrite, CancellationToken token, bool disposeOutput = false,
            int bufferSize = StdLookUps.DefaultBufferSize, Encoding encoding = null)
        {
            return TransformChunks(streamToWrite, transform, input.Length, encoding ?? Encoding.UTF8,
                token, disposeOutput, bufferSize, input.CopyTo);
        }

        private static async Task TransformChunks(Stream writable, ICryptoTransform transform, 
            int length, Encoding enc, CancellationToken token, bool disposeOutput, int chunkSize,
            Action<int, char[], int, int> copyToAction)
        {
            using (var outputWrapper = new WrappedStream(writable, disposeOutput))
            {
                using (var transformer = new CryptoStream(outputWrapper, transform,
                    CryptoStreamMode.Write))
                {
                    var bytes = enc.GetPreamble();
                    if (bytes.Length > 0)
                    {
                        await transformer.WriteAsync(bytes, 0, bytes.Length, token)
                            .ConfigureAwait(false);
                    }
                    var charArr = new char[chunkSize];
                    bytes = new byte[enc.GetMaxByteCount(chunkSize)];
                    var charCnt = length;
                    var position = 0;
                    while (charCnt > 0)
                    {
                        if (charCnt > chunkSize) charCnt = chunkSize;
                        copyToAction(position, charArr, 0, charCnt);
                        var byteCnt = enc.GetBytes(charArr, 0, charCnt, bytes, 0);
                        await transformer.WriteAsync(bytes, 0, byteCnt, token).ConfigureAwait(false);
                        position += charCnt;
                        charCnt = length - position;
                    }
                    await transformer.FlushAsync(token).ConfigureAwait(false);
                    await outputWrapper.FlushAsync(token).ConfigureAwait(false);
                    await writable.FlushAsync(token).ConfigureAwait(false);
                }
            }
        }
    }
}