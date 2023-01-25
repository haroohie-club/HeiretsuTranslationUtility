using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Util;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace HaruhiHeiretsuCLI
{
    public class CheckBlnBinIntegrityCommand : Command
    {
        private string _mcb, _dat, _evt, _grp, _scr;
        public CheckBlnBinIntegrityCommand() : base("check-bln-bin-integrity", "Checks if BLN and BIN files are identical")
        {
            Options = new()
            {
                "Replaces strings in the mcb and dat/evt/scr archives",
                "Usage: HaruhiHeiretsuCLI repalce-strings -m [MCB_PATH] -d [DAT_BIN] -e [EVT_BIN] -s [SCR_BIN] -r [REPLACEMENT_FOLDER] -o [OUTPUT_FOLDER]",
                "",
                { "m|mcb=", "Path to mcb0.bln", m => _mcb = m },
                { "d|dat=", "Path to dat.bin", d => _dat = d },
                { "e|evt=", "Path to evt.bin", e => _evt = e },
                { "g|grp=", "Path to grp.bin", g => _grp = g },
                { "s|scr=", "Path to scr.bin", s => _scr = s },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            McbArchive mcb = Program.GetMcbFile(_mcb);
            BinArchive<FileInArchive> dat = BinArchive<FileInArchive>.FromFile(_dat);
            BinArchive<FileInArchive> evt = BinArchive<FileInArchive>.FromFile(_evt);
            BinArchive<FileInArchive> grp = BinArchive<FileInArchive>.FromFile(_grp);
            BinArchive<FileInArchive> scr = BinArchive<FileInArchive>.FromFile(_scr);

            Dictionary<int, List<(int, int)>> datMap = mcb.GetFileMap(_dat);
            Dictionary<int, List<(int, int)>> evtMap = mcb.GetFileMap(_evt);
            Dictionary<int, List<(int, int)>> grpMap = mcb.GetFileMap(_grp);
            Dictionary<int, List<(int, int)>> scrMap = mcb.GetFileMap(_scr);

            foreach (int datIndex in datMap.Keys)
            {
                string archiveFileHash = string.Join("", SHA256.HashData(Helpers.DecompressData(dat.Files.First(f => f.Index == datIndex).CompressedData)).Select(b => $"{b:X2}"));
                foreach ((int parent, int child) in datMap[datIndex])
                {
                    List<byte> blnSubFileData = mcb.McbSubArchives[parent].Files[child].Data;
                    if (blnSubFileData.Count % 0x10 == 1 && blnSubFileData.Last() == 0x00)
                    {
                        blnSubFileData.RemoveAt(blnSubFileData.Count - 1);
                    }
                    while (blnSubFileData.Count % 0x10 != 0)
                    {
                        blnSubFileData.Add(0x00);
                    }
                    string mcbFileHash = string.Join("", SHA256.HashData(blnSubFileData.ToArray()).Select(b => $"{b:X2}"));
                    if (!archiveFileHash.Equals(mcbFileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        CommandSet.Out.WriteLine($"dat.bin #{datIndex:D4} does not match MCB ({parent}, {child}) ({archiveFileHash}, {mcbFileHash})");
                    }
                }
            }
            CommandSet.Out.WriteLine("Finished verifying dat.bin.");

            foreach (int evtIndex in evtMap.Keys)
            {
                string archiveFileHash = string.Join("", SHA256.HashData(Helpers.DecompressData(evt.Files.First(f => f.Index == evtIndex).CompressedData)).Select(b => $"{b:X2}"));
                foreach ((int parent, int child) in evtMap[evtIndex])
                {
                    List<byte> blnSubFileData = mcb.McbSubArchives[parent].Files[child].Data;
                    if (blnSubFileData.Count % 0x10 == 1 && blnSubFileData.Last() == 0x00)
                    {
                        blnSubFileData.RemoveAt(blnSubFileData.Count - 1);
                    }
                    while (blnSubFileData.Count % 0x10 != 0)
                    {
                        blnSubFileData.Add(0x00);
                    }
                    string mcbFileHash = string.Join("", SHA256.HashData(blnSubFileData.ToArray()).Select(b => $"{b:X2}"));
                    if (!archiveFileHash.Equals(mcbFileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        CommandSet.Out.WriteLine($"evt.bin #{evtIndex:D4} does not match MCB ({parent}, {child}) ({archiveFileHash}, {mcbFileHash})");
                    }
                }
            }
            CommandSet.Out.WriteLine("Finished verifying evt.bin.");

            foreach (int grpIndex in grpMap.Keys)
            {
                string archiveFileHash = string.Join("", SHA256.HashData(Helpers.DecompressData(grp.Files.First(f => f.Index == grpIndex).CompressedData)).Select(b => $"{b:X2}"));
                foreach ((int parent, int child) in grpMap[grpIndex])
                {
                    List<byte> blnSubFileData = mcb.McbSubArchives[parent].Files[child].Data;
                    if (blnSubFileData.Count % 0x10 == 1 && blnSubFileData.Last() == 0x00)
                    {
                        blnSubFileData.RemoveAt(blnSubFileData.Count - 1);
                    }
                    while (blnSubFileData.Count % 0x10 != 0)
                    {
                        blnSubFileData.Add(0x00);
                    }
                    string mcbFileHash = string.Join("", SHA256.HashData(blnSubFileData.ToArray()).Select(b => $"{b:X2}"));
                    if (!archiveFileHash.Equals(mcbFileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        CommandSet.Out.WriteLine($"grp.bin #{grpIndex:D4} does not match MCB ({parent}, {child}) ({archiveFileHash}, {mcbFileHash})");
                    }
                }
            }
            CommandSet.Out.WriteLine("Finished verifying grp.bin.");

            foreach (int scrIndex in scrMap.Keys)
            {
                string archiveFileHash = string.Join("", SHA256.HashData(Helpers.DecompressData(scr.Files.First(f => f.Index == scrIndex).CompressedData)).Select(b => $"{b:X2}"));
                foreach ((int parent, int child) in scrMap[scrIndex])
                {
                    List<byte> blnSubFileData = mcb.McbSubArchives[parent].Files[child].Data;
                    if (blnSubFileData.Count % 0x10 == 1 && blnSubFileData.Last() == 0x00)
                    {
                        blnSubFileData.RemoveAt(blnSubFileData.Count - 1);
                    }
                    while (blnSubFileData.Count % 0x10 != 0)
                    {
                        blnSubFileData.Add(0x00);
                    }
                    string mcbFileHash = string.Join("", SHA256.HashData(blnSubFileData.ToArray()).Select(b => $"{b:X2}"));
                    if (!archiveFileHash.Equals(mcbFileHash, StringComparison.OrdinalIgnoreCase))
                    {
                        CommandSet.Out.WriteLine($"scr.bin #{scrIndex:D4} does not match MCB ({parent}, {child}) ({archiveFileHash}, {mcbFileHash})");
                    }
                }
            }
            CommandSet.Out.WriteLine("Finished verifying scr.bin.");

            return 0;
        }
    }
}
