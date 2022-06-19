using HaruhiHeiretsuLib.Strings;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace HaruhiHeiretsuTests
{
    public class ScriptTests
    {
        private const string SCRCOMMAND_FILE = @"inputs\SCRCOMMAND.bin";

        private const string SCR001 = @"inputs\SCRSCR_0_01_A.bin";
        private const string SCR101 = @"inputs\SCRSCR_1_01_A.bin";
        private const string SCRKWMT = @"inputs\SCRTEST_KWMT.bin";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase(SCR001)]
        [TestCase(SCR101)]
        [TestCase(SCRKWMT)]
        public void ScriptCompileDecompileTrueInverses(string file)
        {
            List<ScriptCommand> commands = ScriptCommand.ParseScriptCommandFile(File.ReadAllBytes(SCRCOMMAND_FILE));
            byte[] dataOnDisk = File.ReadAllBytes(file);
            ScriptFile script = new(0, 0, dataOnDisk);
            script.AvailableCommands = commands;
            script.PopulateCommandBlocks();
            string scriptCode = script.Decompile();
            ScriptFile newScript = new();
            newScript.AvailableCommands = commands;
            newScript.Compile(scriptCode);

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            File.WriteAllBytes($"output\\{Path.GetFileNameWithoutExtension(file)}-compiled.bin", newScript.GetBytes());
            File.WriteAllText($"output\\{Path.GetFileNameWithoutExtension(file)}-initial.sws", scriptCode);
            File.WriteAllText($"output\\{Path.GetFileNameWithoutExtension(file)}-final.sws", newScript.Decompile());
            Assert.AreEqual(dataOnDisk, newScript.GetBytes());
        }
    }
}