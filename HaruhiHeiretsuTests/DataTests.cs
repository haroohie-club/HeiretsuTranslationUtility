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

            File.WriteAllBytes("output/camdef-initial.bin", initialBytes);
            File.WriteAllBytes("output/camdef-generated.bin", generatedBytes);

            Assert.AreEqual(initialCsv, generatedCsv);
            Assert.AreEqual(initialBytes, generatedBytes);
        }
    }
}