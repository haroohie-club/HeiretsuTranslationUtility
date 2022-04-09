﻿using System.IO;
using Kontract.Kompression.Configuration;
#pragma warning disable 649

namespace plugin_shade.Archives
{
    // Archive index maps to the following bins for Inazuma Eleven Strikers 2013
    // 0x00 => strap.bin
    // 0x01 => scn.bin
    // 0x02 => scn_sh.bin
    // 0x03 => ui.bin
    // 0x04 => dat.bin
    // 0x05 => grp.bin?

    // For Suzumiya Haruhi no Heiretsu
    // 0x00 => grp.bin
    // 0x01 => dat.bin
    // 0x02 => scr.bin
    // 0x03 => evt.bin

    public class BlnSubEntry
    {
        public int archiveIndex;    // index to an external bin
        public int archiveOffset;   // offset into that external bin
        public int size;
    }


    public class BlnSubArchiveFileInfo : ShadeArchiveFileInfo
    {
        public long Offset { get; }
        public BlnSubEntry Entry { get; }

        public BlnSubArchiveFileInfo(Stream fileData, string filePath, BlnSubEntry entry, long offset) :
            base(fileData, filePath)
        {
            Offset = offset;
            Entry = entry;
        }

        public BlnSubArchiveFileInfo(Stream fileData, string filePath, BlnSubEntry entry, IKompressionConfiguration configuration, long decompressedSize, long offset) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Offset = offset;
            Entry = entry;
        }

        
    }
}
