using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Drawing;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;

namespace NINATest.Utility {

    [TestFixture]
    public class BlittableTest {

        [Test]
        public void Primitives_Blittable() {
            Assert.IsTrue(Blittable<int>.IsBlittable);
            Assert.IsTrue(Blittable<uint>.IsBlittable);
            Assert.IsTrue(Blittable<float>.IsBlittable);
        }

        [Test]
        public void Array_of_Primitives_Blittable() {
            Assert.IsTrue(Blittable<int[]>.IsBlittable);
            Assert.IsTrue(Blittable<uint[]>.IsBlittable);
            Assert.IsTrue(Blittable<float[]>.IsBlittable);
        }

        [Test]
        public void Bool_Not_Blittable() {
            // Bool is the primitive that isn't blittable
            Assert.IsFalse(Blittable<bool>.IsBlittable);
            Assert.IsFalse(Blittable<bool[]>.IsBlittable);
        }

        [Test]
        public void Struct_and_Class_with_layout_Blittable() {
            // Both classes and structs (reference and value) are blittable if they are laid out property, with StructLayout if necessary
            Assert.IsTrue(Blittable<Point>.IsBlittable);
            Assert.IsTrue(Blittable<SBIG.QueryCommandStatusParams>.IsBlittable);
        }

        [Test]
        public void Array_of_Struct_with_layout_Blittable() {
            // An array of blittable value types is also blittable
            Assert.IsTrue(Blittable<Point[]>.IsBlittable);
        }

        [Test]
        public void Array_of_Class_with_layout_Not_Blittable() {
            // However, an array of reference types is not blittable, even if the element type is
            Assert.IsFalse(Blittable<SBIG.QueryCommandStatusParams[]>.IsBlittable);
        }

        [Test]
        public void Managed_Class_Not_Blittable() {
            Assert.IsFalse(Blittable<SBIG.FailedOperation>.IsBlittable);
        }
    }
}
