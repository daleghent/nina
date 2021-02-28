#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NUnit.Framework;

namespace NINATest.Utility {
    [TestFixture]
    public class ObservableRectangleTest {
        [Test]
        public void Rotation_NoInitialOffset() {
            //Arrange
            var rotation = 45d;
            ObservableRectangle rectangle = new ObservableRectangle(0);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(rotation, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void RotationAbove360_NoInitialOffset() {
            //Arrange
            var rotation = 400d;
            ObservableRectangle rectangle = new ObservableRectangle(0);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation - 360, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(rotation - 360, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void Rotation_InitialOffsetSet() {
            //Arrange
            var rotation = 45d;
            var initialRotation = 15d;
            ObservableRectangle rectangle = new ObservableRectangle(initialRotation);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(rotation + initialRotation, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void RotationAbove360_InitialOffsetSet() {
            //Arrange
            var rotation = 400d;
            var initialRotation = 15d;
            ObservableRectangle rectangle = new ObservableRectangle(initialRotation);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation - 360, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(rotation + initialRotation - 360, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void RotationNegative_NoInitialOffsetSet() {
            //Arrange
            var rotation = 400d;
            ObservableRectangle rectangle = new ObservableRectangle(0);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation - 360, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(rotation - 360, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void RotationNegative_InitialOffsetSet() {
            //Arrange
            var rotation = -45d;
            var initialRotation = 15d;
            ObservableRectangle rectangle = new ObservableRectangle(initialRotation);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation + 360, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(rotation + initialRotation + 360, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void TotalRotationAbove360_InitialOffsetSet() {
            //Arrange
            var rotation = 355d;
            var initialRotation = 15d;
            ObservableRectangle rectangle = new ObservableRectangle(initialRotation);

            //Act
            rectangle.Rotation = rotation;

            //Assert
            Assert.AreEqual(rotation, rectangle.Rotation, "Invalid rotation value");
            Assert.AreEqual(10, rectangle.TotalRotation, "Invalid total rotation value");
        }

        [Test]
        public void TotalRotation_NoInitialOffsetNoRotation() {
            //Arrange
            var rotation = 45d;
            ObservableRectangle rectangle = new ObservableRectangle(0);

            //Act
            rectangle.TotalRotation = rotation;

            //Assert
            Assert.AreEqual(rotation, rectangle.Rotation, "Invalid rotation value");
        }

        [Test]
        public void TotalRotation_InitialOffsetSetNoRotation() {
            //Arrange
            var rotation = 45d;
            var initialOffset = 15d;
            ObservableRectangle rectangle = new ObservableRectangle(initialOffset);

            //Act
            rectangle.TotalRotation = rotation;

            //Assert
            Assert.AreEqual(rotation - initialOffset, rectangle.Rotation, "Invalid rotation value");
        }
        
        [Test]
        public void TotalRotation_InitialOffsetSetRotationSet() {
            //Arrange
            var rotation = 45d;
            var initialOffset = 15d;
            ObservableRectangle rectangle = new ObservableRectangle(initialOffset);
            rectangle.Rotation = 300d;

            //Act
            rectangle.TotalRotation = rotation;

            //Assert
            Assert.AreEqual(rotation - initialOffset, rectangle.Rotation, "Invalid rotation value");
        }

        [Test]
        public void Rotation_PropertyChangedFired() {
            //Arrange
            ObservableRectangle rectangle = new ObservableRectangle(0);
           
            var propertyChangedFired = false;
            rectangle.PropertyChanged += (obj, events) => {
                propertyChangedFired = true;
            };

            //Act
            rectangle.Rotation = 15;
            
            //Assert
            Assert.AreEqual(true, propertyChangedFired, "PropertyChangedEventNotFired");
        }
        
        [Test]
        public void TotalRotation_PropertyChangedFired() {
            //Arrange
            ObservableRectangle rectangle = new ObservableRectangle(0);
           
            var propertyChangedFired = false;
            rectangle.PropertyChanged += (obj, events) => {
                propertyChangedFired = true;
            };

            //Act
            rectangle.TotalRotation = 15;
            
            //Assert
            Assert.AreEqual(true, propertyChangedFired, "PropertyChangedEventNotFired");
        }
    }
}