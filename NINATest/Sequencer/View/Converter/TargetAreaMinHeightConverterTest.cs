#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.View.Sequencer.Converter;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NINATest.Sequencer.View.Converter {

    [TestFixture]
    public class TargetAreaMinHeightConverterTest {
        //[Test]
        //[Apartment(ApartmentState.STA)]
        //public async Task Convert_3ContainerInControl_ReturnZero() {
        //    var viewportHeight = 100d;

        //    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        //    var control = new ListBox();
        //    control.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, false);
        //    control.ItemContainerGenerator.StatusChanged += (object sender, EventArgs e) => {
        //        //Won't be reached. not sure how to force it.
        //        tcs.SetResult(true);
        //    };

        //    var items = new List<object>() { new object(), new object(), new object() };
        //    var actualHeight = 50d;
        //    control.Items.Add(new object());
        //    control.Items.Add(new object());
        //    control.Items.Add(new object());
        //    control.UpdateLayout();

        //    await tcs.Task;

        //    var sut = new TargetAreaMinHeightConverter();

        //    var conversion = sut.Convert(new object[] { viewportHeight, control, actualHeight }, default, default, default);

        //    conversion.Should().BeOfType<int>();
        //    conversion.Should().Be(0);
        //}

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Convert_NoContainerInControl_ReturnZero() {
            var viewportHeight = 100d;
            var control = new ItemsControl();
            var actualHeight = 50d;

            var sut = new TargetAreaMinHeightConverter();

            var conversion = sut.Convert(new object[] { viewportHeight, control, actualHeight }, default, default, default);

            conversion.Should().BeOfType<int>();
            conversion.Should().Be(0);
        }

        [Test]
        public void Convert_InvalidArguments_ArgumentException() {
            var sut = new TargetAreaMinHeightConverter();

            Action act = () => sut.Convert(default, default, default, default);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Convert_InvalidArguments2_ArgumentException() {
            var sut = new TargetAreaMinHeightConverter();

            Action act = () => sut.Convert(new object[] { 1, 2 }, default, default, default);

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Convert_InvalidArguments3_InvalidCastException() {
            var sut = new TargetAreaMinHeightConverter();

            Action act = () => sut.Convert(new object[] { 1, 2, 3 }, default, default, default);

            act.Should().Throw<InvalidCastException>();
        }

        [Test]
        public void Convert_InvalidArguments4_InvalidCastException() {
            var sut = new TargetAreaMinHeightConverter();

            Action act = () => sut.Convert(new object[] { 1d, 2d, 3d }, default, default, default);

            act.Should().Throw<InvalidCastException>();
        }

        [Test]
        public void ConvertBack_NotImplemented() {
            var sut = new TargetAreaMinHeightConverter();

            Action act = () => sut.ConvertBack(default, default, default, default);

            act.Should().Throw<NotImplementedException>();
        }
    }
}