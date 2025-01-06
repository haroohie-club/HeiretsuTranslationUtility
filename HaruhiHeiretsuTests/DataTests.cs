using HaruhiHeiretsuLib.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace HaruhiHeiretsuTests
{
    public class DataTests
    {
        private const string MapDefinitionsFilePath = @"inputs/mapdef.bin";
        private const string CameraDataFilePath = @"inputs/cameradata.bin";
        private const string TopicsFlagsFilePath = @"inputs/topicsFlags.bin";
        private const string NameplatesFilePath = @"inputs/nameplates.bin";
        private const string TimelineFilePath = @"inputs/timeline.bin";
        private const string ClubroomFilePath = @"inputs/clubroom.bin";
        private const string ExtrasClfClaFilePath = @"inputs/extrasclfcla.bin";
        private const string ExtrasCldFilePath = @"inputs/extrascld.bin";

        [SetUp]
        public void Setup()
        {
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
        }

        [Test]
        public void MapDefFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(MapDefinitionsFilePath);
            MapDefinitionsFile mapDefinitionsFile = new();
            mapDefinitionsFile.Initialize(initialBytes, 0);
            string initialCsv = mapDefinitionsFile.GetCsv();

            byte[] generatedBytes = mapDefinitionsFile.GetBytes();
            MapDefinitionsFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);
            string generatedCsv = generatedFile.GetCsv();

            Assert.That(initialCsv, Is.EquivalentTo(generatedCsv));
            if (generatedBytes.Length < initialBytes.Length)
            {
                List<byte> temp = [.. generatedBytes];
                temp.AddRange(new byte[initialBytes.Length - generatedBytes.Length]);
                generatedBytes= [.. temp];
            }
            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void CameraDataFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(CameraDataFilePath);
            CameraDataFile cameraDataFile = new();
            cameraDataFile.Initialize(initialBytes, 0);
            string initialCsv = cameraDataFile.GetCsv();

            byte[] generatedBytes = cameraDataFile.GetBytes();
            CameraDataFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);
            string generatedCsv = generatedFile.GetCsv();

            Assert.That(initialCsv, Is.EquivalentTo(generatedCsv));
            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void TopicsFlagsFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(TopicsFlagsFilePath);
            TopicsAndFlagsFile topicsAndFlagsFile = new();
            topicsAndFlagsFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = topicsAndFlagsFile.GetBytes();
            TopicsAndFlagsFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);

            File.WriteAllBytes("output/topicsFlags-gen.bin", generatedBytes);

            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void NameplatesFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(NameplatesFilePath);
            NameplatesFile nameplatesFile = new();
            nameplatesFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = nameplatesFile.GetBytes();
            NameplatesFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);

            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void TimelineFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(TimelineFilePath);
            TimelineFile timelineFile = new();
            timelineFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = timelineFile.GetBytes();
            TimelineFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);

            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void ClubroomFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(ClubroomFilePath);
            ClubroomKoizumiCutscenesFile clubroomCutsceneFile = new();
            clubroomCutsceneFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = clubroomCutsceneFile.GetBytes();
            ClubroomKoizumiCutscenesFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);
            
            File.WriteAllBytes("output/clubroom-gen.bin", generatedBytes);

            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void ExtrasClfClaFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(ExtrasClfClaFilePath);
            ClubroomHaruhiModelsFile extrasClfClaFile = new();
            extrasClfClaFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = extrasClfClaFile.GetBytes();
            ClubroomHaruhiModelsFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);

            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }

        [Test]
        public void ExtrasCldFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(ExtrasCldFilePath);
            ClubroomNagatoDatabaseFile extrasCldFile = new();
            extrasCldFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = extrasCldFile.GetBytes();
            File.WriteAllBytes("output/extrascld-initial.bin", initialBytes);
            File.WriteAllBytes("output/extrascld-generated.bin", generatedBytes);
            ClubroomNagatoDatabaseFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);

            Assert.That(initialBytes, Is.EquivalentTo(generatedBytes));
        }
    }
}