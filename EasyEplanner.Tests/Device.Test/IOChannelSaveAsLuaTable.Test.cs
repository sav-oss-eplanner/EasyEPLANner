using EplanDevice;
using NUnit.Framework;

namespace EasyEplannerTests.DeviceTest
{
    public class IOChannelSaveAsLuaTableTest
    {
        [Test]
        public void SaveAsLuaTable_UnboundChannel_PreservesPlaceholderPosition()
        {
            var channel = new IODevice.IOChannel("DI", -1, -1, -1,
                "reserve position");

            string actual = channel.SaveAsLuaTable("    ");

            Assert.AreEqual(
                "    {\n" +
                "    -- reserve position\n" +
                "    node          = -1,\n" +
                "    offset        = -1,\n" +
                "    physical_port = -1,\n" +
                "    logical_port  = -1,\n" +
                "    module_offset = -1\n" +
                "    },\n",
                actual);
        }

        [Test]
        public void SaveAsLuaTable_UnboundChannelWithoutComment_OmitsCommentLine()
        {
            var channel = new IODevice.IOChannel("AO", -1, -1, -1,
                string.Empty);

            string actual = channel.SaveAsLuaTable(string.Empty);

            StringAssert.DoesNotContain("--", actual);
            StringAssert.Contains("node          = -1,", actual);
            StringAssert.Contains("offset        = -1,", actual);
            StringAssert.Contains("physical_port = -1,", actual);
            StringAssert.Contains("logical_port  = -1,", actual);
            StringAssert.Contains("module_offset = -1", actual);
        }
    }
}
