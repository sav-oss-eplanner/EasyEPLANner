using EasyEPlanner;
using EasyEPlanner.FileSavers.XML;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace EasyEplannerTests.FileSaverTest.XMLTest
{
    [TestFixture]
    public class ChannelBaseAnalysisTest
    {
        [Test]
        public void Statistics_CountsSubtypesAndChannelsAndFindsLargestSubtype()
        {
            var first = new Subtype("SYSTEM");
            first.Channels.Add(new Channel("SYSTEM.UP_TIME"));
            var second = new Subtype("OBJECT1");
            second.Channels.Add(new Channel("OBJECT1.CMD"));
            second.Channels.Add(new Channel("OBJECT1.ST"));

            var statistics = new ChannelBaseSubtypeStatistics("TestBase",
                new List<ISubtype> { first, second });

            Assert.Multiple(() =>
            {
                Assert.AreEqual("TestBase", statistics.BaseName);
                Assert.AreEqual(2, statistics.SubtypeCount);
                Assert.AreEqual(3, statistics.TotalChannelCount);
                Assert.AreEqual(2, statistics.MaxChannelsPerSubtype);
                Assert.AreEqual("OBJECT1", statistics.MaxChannelsSubtypeName);
            });
        }

        [Test]
        public void ExportAnalysis_AcceptsSubtypeAndChannelCapacityBoundaries()
        {
            var subtypes = new List<ISubtype>();
            for (int i = 0; i < XmlReporter.MaximumSubtypeCount; i++)
            {
                subtypes.Add(new Subtype($"ST{i}"));
            }

            var maximumSubtypeStatistics = new ChannelBaseSubtypeStatistics(
                "Boundary", subtypes);
            var analysis = new ChannelBaseExportAnalysis(
                new List<ChannelBaseSubtypeStatistics> { maximumSubtypeStatistics });

            Assert.AreEqual(1, analysis.Bases.Count);
            Assert.IsTrue(analysis.IsWithinLimits);
            Assert.IsFalse(analysis.ExceedsSubtypeLimit);
            Assert.IsFalse(analysis.ExceedsChannelLimit);

            var channelSubtype = new Subtype("MAX_CHANNELS");
            var channel = new Channel("MAX_CHANNELS.TAG");
            for (int i = 0; i < XmlReporter.MaximumChannelCountPerSubtype; i++)
            {
                channelSubtype.Channels.Add(channel);
            }

            var channelStatistics = new ChannelBaseSubtypeStatistics("Channels",
                new List<ISubtype> { channelSubtype });
            analysis = new ChannelBaseExportAnalysis(
                new List<ChannelBaseSubtypeStatistics> { channelStatistics });

            Assert.IsTrue(analysis.IsWithinLimits);
            Assert.AreEqual(XmlReporter.MaximumChannelCountPerSubtype,
                channelStatistics.MaxChannelsPerSubtype);
        }

        [Test]
        public void ExportAnalysis_RejectsCountsAboveEitherCapacity()
        {
            var subtypes = new List<ISubtype>();
            for (int i = 0; i <= XmlReporter.MaximumSubtypeCount; i++)
            {
                subtypes.Add(new Subtype($"ST{i}"));
            }

            var tooManyChannelsSubtype = new Subtype("TOO_MANY_CHANNELS");
            var channel = new Channel("TOO_MANY_CHANNELS.TAG");
            for (int i = 0; i <= XmlReporter.MaximumChannelCountPerSubtype; i++)
            {
                tooManyChannelsSubtype.Channels.Add(channel);
            }

            var analysis = new ChannelBaseExportAnalysis(new List<ChannelBaseSubtypeStatistics>
            {
                new ChannelBaseSubtypeStatistics("TooManySubtypes", subtypes),
                new ChannelBaseSubtypeStatistics("TooManyChannels",
                    new List<ISubtype> { tooManyChannelsSubtype })
            });

            Assert.IsTrue(analysis.ExceedsSubtypeLimit);
            Assert.IsTrue(analysis.ExceedsChannelLimit);
            Assert.IsFalse(analysis.IsWithinLimits);
        }

        [Test]
        public void AnalysisRunner_UsesExportCultureAndRestoresCallerCulture()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            var callerCulture = new CultureInfo("ru-RU");
            try
            {
                Thread.CurrentThread.CurrentCulture = callerCulture;

                double parsedValue = ChannelBaseAnalysisRunner.Run(
                    () => Convert.ToDouble("12.5"));

                Assert.AreEqual(12.5, parsedValue);
                Assert.AreSame(callerCulture, Thread.CurrentThread.CurrentCulture);

                Assert.Throws<FormatException>(() => ChannelBaseAnalysisRunner.Run(
                    () => Convert.ToDouble("not-a-number")));
                Assert.AreSame(callerCulture, Thread.CurrentThread.CurrentCulture);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void AnalysisRunner_RejectsNullDelegate()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ChannelBaseAnalysisRunner.Run<int>(null));
        }

        [Test]
        public void ChannelId_UsesDifferentBaseBytesAndExpectedSubtypeAndChannelBytes()
        {
            long technologyChannelId = ChannelBaseIdGenerator.Generate(1, 4, 9);
            long deviceChannelId = ChannelBaseIdGenerator.Generate(2, 4, 9);

            Assert.AreEqual(0x01040009, technologyChannelId);
            Assert.AreEqual(0x02040009, deviceChannelId);
            Assert.AreNotEqual(technologyChannelId, deviceChannelId);
            Assert.AreEqual(0x02FFFFFF,
                ChannelBaseIdGenerator.Generate(2, byte.MaxValue, ushort.MaxValue));
        }

        [TestCase(-1, 0, 0, "driverId")]
        [TestCase(256, 0, 0, "driverId")]
        [TestCase(1, -1, 0, "subtypeId")]
        [TestCase(1, 256, 0, "subtypeId")]
        [TestCase(1, 0, -1, "channelId")]
        [TestCase(1, 0, 65536, "channelId")]
        public void ChannelId_RejectsOutOfRangeComponents(int driverId,
            int subtypeId, int channelId, string parameterName)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ChannelBaseIdGenerator.Generate(driverId, subtypeId, channelId));
            Assert.AreEqual(parameterName, exception.ParamName);
        }
    }
}
