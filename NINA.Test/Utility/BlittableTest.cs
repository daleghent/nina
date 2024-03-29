﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Drawing;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NUnit.Framework.Legacy;

namespace NINA.Test.Utility {

    [TestFixture]
    public class BlittableTest {

        [Test]
        public void Primitives_Blittable() {
            ClassicAssert.IsTrue(Blittable<int>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<uint>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<float>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<bool>.IsBlittable);
        }

        [Test]
        public void Array_of_Primitives_Blittable() {
            ClassicAssert.IsTrue(Blittable<int[]>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<uint[]>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<float[]>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<bool[]>.IsBlittable);
        }

        [Test]
        public void Struct_and_Class_with_layout_Blittable() {
            // Both classes and structs (reference and value) are blittable if they are laid out property, with StructLayout if necessary
            ClassicAssert.IsTrue(Blittable<Point>.IsBlittable);
            ClassicAssert.IsTrue(Blittable<SBIG.QueryCommandStatusParams>.IsBlittable);
        }

        [Test]
        public void Array_of_Struct_with_layout_Blittable() {
            // An array of blittable value types is also blittable
            ClassicAssert.IsTrue(Blittable<Point[]>.IsBlittable);
        }

        [Test]
        public void Array_of_Class_with_layout_Not_Blittable() {
            // However, an array of reference types is not blittable, even if the element type is
            ClassicAssert.IsFalse(Blittable<SBIG.QueryCommandStatusParams[]>.IsBlittable);
        }

        [Test]
        public void Managed_Class_Not_Blittable() {
            ClassicAssert.IsFalse(Blittable<SBIG.FailedOperation>.IsBlittable);
        }
    }
}