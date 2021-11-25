using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace NINATest.Utility {

    [TestFixture]
    public class TextEncodingTest {

        [Test]
        public void UnicodeToAsciiNullTest() {
            // Arrange
            string unicode = null;
            string ascii = "";

            // Act
            string result = TextEncoding.UnicodeToAscii(unicode);

            // Assert
            Assert.AreEqual(ascii, result);
        }

        [Test]
        public void UnicodeToAsciiEmptyTest() {
            // Arrange
            string unicode = "";
            string ascii = "";

            // Act
            string result = TextEncoding.UnicodeToAscii(unicode);

            // Assert
            Assert.AreEqual(ascii, result);
        }

        [Test]
        public void UnicodeToAsciiTest() {
            // Arrange
            string unicode = "TĤis is a ștring with unicódè characterȿ.";
            string ascii = "T?is is a ?tring with unic?d? character?.";

            // Act
            string result = TextEncoding.UnicodeToAscii(unicode);

            // Assert
            Assert.AreEqual(ascii, result);
        }

        [Test]
        public void GreekToLatinAbbreviationNullTest() {
            // Arrange
            string hasGreek = null;
            string inLatin = "";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasGreek);

            // Assert
            Assert.AreEqual(inLatin, result);
        }

        [Test]
        public void GreekToLatinAbbreviationEmptyTest() {
            // Arrange
            string hasGreek = "";
            string inLatin = "";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasGreek);

            // Assert
            Assert.AreEqual(inLatin, result);
        }

        [Test]
        public void GreekToLatinAbbreviationTest() {
            // Arrange
            string hasGreek = "Σ 2103, π Hercules";
            string inLatin = "sig 2103, pi. Hercules";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasGreek);

            // Assert
            Assert.AreEqual(inLatin, result);
        }

        [Test]
        public void LatinToLatinTest() {
            // Arrange
            string hasOnylLatin = "abcdefghijklmnopqrstuvwxyzäöüABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜ1234567890!\"§$%&/()=?`*'_:;><-.,#+@²³{[]}\\~|'";
            string inLatin = "abcdefghijklmnopqrstuvwxyzäöüABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜ1234567890!\"§$%&/()=?`*'_:;><-.,#+@²³{[]}\\~|'";

            // Act
            string result = TextEncoding.GreekToLatinAbbreviation(hasOnylLatin);

            // Assert
            Assert.AreEqual(inLatin, result);
        }
    }
}