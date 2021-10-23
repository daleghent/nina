using FluentAssertions;
using NINA.Profile;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINATest.ProfileTest {

    [TestFixture]
    public class PluginSettingsTest {

        [Test]
        public void TryGetTypeOfField_GuidDoesNotExist_NoSuccessAndTypeIsNull() {
            var sut = new PluginSettings();

            var success = sut.TryGetTypeOfField(Guid.NewGuid(), "SomeKey", out var type);

            success.Should().BeFalse();
            type.Should().BeNull();
        }

        [Test]
        public void TryGetTypeOfField_GuidExist_KeyDoesNotExist_NoSuccessAndTypeIsNull() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();
            sut.SetValue(id, "Something", 10);

            var success = sut.TryGetTypeOfField(id, "SomeKey", out var type);

            success.Should().BeFalse();
            type.Should().BeNull();
        }

        [Test]
        public void TryGetTypeOfField_GuidExist_KeyExist_SuccessAndTypeIsReturned() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();
            sut.SetValue(id, "SomeKey", 10d);

            var success = sut.TryGetTypeOfField(id, "SomeKey", out var type);

            success.Should().BeTrue();
            type.Should().Be(typeof(double));
        }

        [Test]
        public void SetValue_TryGetValueSuccessful_Boolean() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();

            sut.SetValue(id, "SomeKey", true);

            var success = sut.TryGetValue(id, "SomeKey", out bool value);
            success.Should().BeTrue();
            value.Should().BeTrue();
        }

        [Test]
        public void SetValue_TryGetValue_IncorrectType_Unsuccessful() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();

            sut.SetValue(id, "SomeKey", true);

            var success = sut.TryGetValue(id, "SomeKey", out int value);
            success.Should().BeFalse();
            value.Should().Be(default(int));
        }

        [Test]
        public void TryGetValue_GuidDoesNotExist_Unsuccessful() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();

            var success = sut.TryGetValue(id, "SomeKey", out int value);
            success.Should().BeFalse();
            value.Should().Be(default(int));
        }

        [Test]
        public void TryGetValue_KeyDoesNotExist_Unsuccessful() {
            var sut = new PluginSettings();
            var id = Guid.NewGuid();
            sut.SetValue(id, "Something", 5);

            var success = sut.TryGetValue(id, "SomeKey", out int value);
            success.Should().BeFalse();
            value.Should().Be(default(int));
        }
    }
}