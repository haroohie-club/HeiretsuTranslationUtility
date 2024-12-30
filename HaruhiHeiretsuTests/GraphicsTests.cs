using HaruhiHeiretsuLib.Graphics;
using NUnit.Framework;
using System.IO;

namespace HaruhiHeiretsuTests
{
    public class GraphicsTests
    {
        private const string UiLayoutPath = @"inputs/layout_ui.bin";

        [SetUp]
        public void Setup()
        {
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }
        }

        [Test]
        [TestCase(UiLayoutPath)]
        public void LayoutCsvTrueInverse(string path)
        {
            byte[] bytesOnDisk = File.ReadAllBytes(path);
            GraphicsFile startLayoutFile = new();
            startLayoutFile.Initialize(bytesOnDisk, 0);
            string startLayoutCsv = startLayoutFile.GetLayoutJson();

            GraphicsFile importedLayoutFile = new();
            importedLayoutFile.Initialize(bytesOnDisk, 0);
            importedLayoutFile.ImportLayoutJson(startLayoutCsv);
            importedLayoutFile.SetLayoutData();

            File.WriteAllBytes(@$"output/{Path.GetFileNameWithoutExtension(path)}-start.bin", bytesOnDisk);
            File.WriteAllBytes(@$"output/{Path.GetFileNameWithoutExtension(path)}-imported.bin", importedLayoutFile.GetBytes());

            Assert.That(bytesOnDisk, Is.EquivalentTo(importedLayoutFile.GetBytes()));
        }
    }
}
