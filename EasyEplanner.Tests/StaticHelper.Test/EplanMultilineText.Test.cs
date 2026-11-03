using NUnit.Framework;
using StaticHelper;

namespace EasyEplannerTests.StaticHelperTest
{
    public class EplanMultilineTextTest
    {
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("single", "single")]
        [TestCase("first\nsecond", "first · second")]
        [TestCase("first\r\nsecond", "first · second")]
        [TestCase("first\u00B6second", "first · second")]
        [TestCase("  first  \n  second  ", "first · second")]
        public void FormatForCell_JoinsLinesWithMiddleDot(string input,
            string expected)
        {
            Assert.AreEqual(expected, EplanMultilineText.FormatForCell(input));
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("single", "single")]
        [TestCase("first\nsecond", "first\r\nsecond")]
        [TestCase("first\u00B6second", "first\r\nsecond")]
        public void FormatForTooltip_PreservesLineBreaks(string input,
            string expected)
        {
            Assert.AreEqual(expected,
                EplanMultilineText.FormatForTooltip(input));
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("single", "single")]
        [TestCase("first\u00B6second", "first\r\nsecond")]
        [TestCase("first\r\nsecond", "first\r\nsecond")]
        [TestCase("first\nsecond", "first\r\nsecond")]
        public void FormatForEditor_ConvertsSeparatorsToLineBreaks(string input,
            string expected)
        {
            Assert.AreEqual(expected,
                EplanMultilineText.FormatForEditor(input));
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("single", "single")]
        [TestCase("first\r\nsecond", "first\u00B6second")]
        [TestCase("first\nsecond", "first\u00B6second")]
        [TestCase("first\rsecond", "first\u00B6second")]
        public void ParseFromEditor_ConvertsLineBreaksToParagraphSign(
            string input, string expected)
        {
            Assert.AreEqual(expected,
                EplanMultilineText.ParseFromEditor(input));
        }

        [Test]
        public void ParseFromEditor_WithCustomSeparator_ConvertsLineBreaksToSeparator()
        {
            Assert.AreEqual("first\r\nsecond",
                EplanMultilineText.ParseFromEditor("first\nsecond", "\r\n"));
        }

        [Test]
        public void ParseFromEditor_ThenFormatForEditor_RoundTripsEplanText()
        {
            const string eplanText = "line1\u00B6line2\u00B6line3";
            var editorText = EplanMultilineText.FormatForEditor(eplanText);
            var restored = EplanMultilineText.ParseFromEditor(editorText);

            Assert.AreEqual(eplanText, restored);
        }

        [Test]
        public void IsSameFunctionalText_TreatsParagraphSignAndCrLfAsEqual()
        {
            Assert.IsTrue(EplanMultilineText.IsSameFunctionalText(
                "line1\u00B6line2",
                "line1\r\nline2"));
        }

        [Test]
        public void IsSameDescription_TreatsEplanLineBreaksAsLuaDotSpace()
        {
            Assert.IsTrue(EplanMultilineText.IsSameDescription(
                "first\u00B6second", "first. second"));
            Assert.IsTrue(EplanMultilineText.IsSameDescription(
                "first\nsecond", "first. second"));
            Assert.IsTrue(EplanMultilineText.IsSameDescription(
                "first\r\nsecond", "first. second"));
            Assert.IsFalse(EplanMultilineText.IsSameDescription(
                "first. second", "other"));
        }

        [Test]
        public void NormalizeDescription_JoinsLinesWithDotSpace()
        {
            Assert.AreEqual("first. second",
                EplanMultilineText.NormalizeDescription("first\u00B6second"));
            Assert.AreEqual("", EplanMultilineText.NormalizeDescription(null));
        }
    }
}
