using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Models.Archive;

namespace plugin_shade.Archives
{
    // Game: Inazuma Eleven GO Strikers 2013
    // HINT: Despite being on Wii, this archive is Little Endian
    // HINT: Unbelievably ugly archive. Ignore everything that's done here and move on with your life, god dammit
    public class BlnSub
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using BinaryReaderX br = new BinaryReaderX(input, true);

            // Read files
            var result = new List<IArchiveFileInfo>();

            int index = 0;
            while (br.BaseStream.Position < input.Length)
            {
                int sample = br.ReadInt32();
                if (sample == 0x7FFF)
                {
                    break;
                }

                br.BaseStream.Position -= 4;
                BlnSubEntry entry = br.ReadType<BlnSubEntry>();

                if (entry.size == 0)
                {
                    break;
                }

                SubStream stream = new SubStream(input, br.BaseStream.Position, entry.size);
                result.Add(CreateAfi(stream, index++, entry, br.BaseStream.Position));

                br.BaseStream.Position += entry.size;
            }

            return result;
        }

        public IArchiveFileInfo GetFile(Stream input, int fileIndex)
        {
            using var br = new BinaryReaderX(input, true);
            IArchiveFileInfo result = null;


            int index = 0;
            while (br.BaseStream.Position < input.Length)
            {
                int sample = br.ReadInt32();
                if (sample == 0x7FFF)
                    break;

                br.BaseStream.Position -= 4;
                BlnSubEntry entry = br.ReadType<BlnSubEntry>();

                if (entry.size == 0)
                    break;
                
                if (index == fileIndex)
                {
                    var stream = new SubStream(input, br.BaseStream.Position, entry.size);
                    result = CreateAfi(stream, index++, entry, br.BaseStream.Position);
                    break;
                }
                else
                {
                    index++;
                    br.BaseStream.Position += entry.size;
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files, int archiveIndexToAdjust = -1, IDictionary<int, int> offsetAdjustments = null, bool leaveOpen = false)
        {
            // Write files
            using var bw = new BinaryWriterX(output, leaveOpen: leaveOpen);
            foreach (var file in files.Cast<BlnSubArchiveFileInfo>())
            {
                long startOffset = output.Position;
                output.Position += 0xC;

                long writtenSize = file.SaveFileData(output);

                if (archiveIndexToAdjust == file.Entry.archiveIndex && offsetAdjustments != null)
                {
                    file.Entry.archiveOffset = offsetAdjustments[file.Entry.archiveOffset];
                }

                file.Entry.size = (int)writtenSize;
                long endOffset = startOffset + writtenSize + 0xC;
                output.Position = startOffset;
                bw.WriteType(file.Entry);

                output.Position = endOffset;
            }

            // Write end entry
            bw.Write(0x7FFF);
            bw.WriteAlignment(0x1000);
        }

        private ArchiveFileInfo CreateAfi(Stream stream, int index, BlnSubEntry entry, long offset)
        {
            // Every file not compressed with the headered Spike Chunsoft compression, is compressed headerless
            var compressionMagic = ShadeSupport.PeekInt32LittleEndian(stream);
            if (compressionMagic != 0xa755aafc)
                return new BlnSubArchiveFileInfo(stream, ShadeSupport.CreateFileName(index, stream, false), entry, Kompression.Implementations.Compressions.ShadeLzHeaderless, ShadeLzHeaderlessDecoder.CalculateDecompressedSize(stream), offset);

            stream.Position = 0;
            return new BlnSubArchiveFileInfo(stream, ShadeSupport.CreateFileName(index, stream, true), entry, Kompression.Implementations.Compressions.ShadeLz, ShadeSupport.PeekDecompressedSize(stream), offset);
        }

        
    }
}
