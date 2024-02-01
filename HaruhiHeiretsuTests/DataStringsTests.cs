using HaruhiHeiretsuLib;
using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Strings.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiHeiretsuTests
{
    public class DataStringsTests
    {
        private const string FontReplacementJson = @"inputs\resx\font_replacement_map.json";

        private const string MapDefinitionsFilePath = @"inputs\mapdef.bin";
        private const string MapDefinitionsResxPath = @"inputs\resx\mapdef.en.resx";
        private const string TopicsFlagsFilePath = @"inputs\topicsFlags.bin";
        private const string NameplatesFilePath = @"inputs\nameplates.bin";
        private const string TimelineFilePath = @"inputs\timeline.bin";
        private const string ClubroomFilePath = @"inputs\clubroom.bin";
        private const string ExtrasClfClaFilePath = @"inputs\extrasclfcla.bin";
        private const string ExtrasCldFilePath = @"inputs\extrascld.bin";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void MapDefFileResxImportReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(MapDefinitionsFilePath);
            FileInArchive mapDefDataFile = new();
            mapDefDataFile.Initialize(initialBytes, 0);

            DataStringsFile<MapDefinitionsFile> mapDefStringsFile = mapDefDataFile.CastTo<DataStringsFile<MapDefinitionsFile>>();
            mapDefStringsFile.ImportResxFile(MapDefinitionsResxPath, FontReplacementMap.FromJson(File.ReadAllText(FontReplacementJson)));
            byte[] generatedBytes = mapDefStringsFile.DataFile.GetBytes();

            if (generatedBytes.Length < initialBytes.Length)
            {
                List<byte> temp = [.. generatedBytes];
                temp.AddRange(new byte[initialBytes.Length - generatedBytes.Length]);
                generatedBytes = [.. temp];
            }
            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }
    }
}
