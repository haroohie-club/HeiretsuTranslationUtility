using HaruhiHeiretsuLib.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HaruhiHeiretsuTests
{
    public class DataTests
    {
        private const string MapDefinitionsFilePath = @"inputs\mapdef.bin";
        private const string CameraDataFilePath = @"inputs\cameradata.bin";
        private const string TopicsFlagsFilePath = @"inputs\topicsFlags.bin";
        private const string NameplatesFilePath = @"inputs\nameplates.bin";
        private const string TimelineFilePath = @"inputs\timeline.bin";

        [SetUp]
        public void Setup()
        {
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

            Assert.AreEqual(initialCsv, generatedCsv);
            if (generatedBytes.Length < initialBytes.Length)
            {
                List<byte> temp = generatedBytes.ToList();
                temp.AddRange(new byte[initialBytes.Length - generatedBytes.Length]);
                generatedBytes= temp.ToArray();
            }
            Assert.AreEqual(initialBytes, generatedBytes);
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

            Assert.AreEqual(initialCsv, generatedCsv);
            Assert.AreEqual(initialBytes, generatedBytes);
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


            Assert.AreEqual(initialBytes, generatedBytes);
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


            Assert.AreEqual(initialBytes, generatedBytes);
        }

        [Test]
        public void TimelineFileParsingIsReversible()
        {
            byte[] initialBytes = File.ReadAllBytes(TimelineFilePath);
            TimelineFile timelienFile = new();
            timelienFile.Initialize(initialBytes, 0);

            byte[] generatedBytes = timelienFile.GetBytes();
            File.WriteAllBytes("output/timeline-initial.bin", initialBytes);
            File.WriteAllBytes("output/timeline-generated.bin", generatedBytes);
            TimelineFile generatedFile = new();
            generatedFile.Initialize(generatedBytes, 0);


            Assert.AreEqual(initialBytes, generatedBytes);
        }
    }
}