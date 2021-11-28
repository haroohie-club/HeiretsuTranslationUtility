﻿using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.Progress;

namespace Kontract.Interfaces.Plugins.State.Intermediate
{
    /// <summary>
    /// Provides methods to compress or decompress files.
    /// </summary>
    public interface ICompressionAdapter : IIntermediate
    {
        /// <summary>
        /// Compresses a file.
        /// </summary>
        /// <param name="toCompress"></param>
        /// <param name="compressInto"></param>
        /// <param name="progress"></param>
        Task<bool> Compress(Stream toCompress, Stream compressInto, IProgressContext progress);

        /// <summary>
        /// Decompresses a file.
        /// </summary>
        /// <param name="toDecompress"></param>
        /// <param name="decompressInto"></param>
        /// <param name="progress"></param>
        Task<bool> Decompress(Stream toDecompress, Stream decompressInto, IProgressContext progress);
    }
}
