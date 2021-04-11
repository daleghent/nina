using NUnit.Framework;
using System;
using NINA.Core.Utility;
using System.Windows.Media.Media3D;
using NINA.Astrometry;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Database;
using System.IO;
using Moq;
using NINA.Profile.Interfaces;
using ASCOM.Astrometry.NOVASCOM;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Interfaces;

namespace NINATest.Dome {

    [TestFixture]
    public class DomeSynchronizationTest {
        private double siteLatitude;
        private double siteLongitude;
        private double localSiderealTime;
        private DatabaseInteraction db;
        private static Angle DEGREES_EPSILON = Angle.ByDegree(0.1);

        [SetUp]
        public void Init() {
            var ninaDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"NINA.sqlite");
            db = new DatabaseInteraction(string.Format(@"Data Source={0};", ninaDbPath));
        }

        private DomeSynchronization Initialize(
            double domeRadius = 1000.0,
            double gemAxisLength = 0.0,
            double lateralAxisLength = 0.0,
            double mountOffsetX = 0.0,
            double mountOffsetY = 0.0,
            double mountOffsetZ = 0.0,
            double siteLatitude = 41.3,
            double siteLongitude = -74.4) {
            var mockProfileService = new Mock<IProfileService>();
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.DomeRadius_mm).Returns(domeRadius);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.GemAxis_mm).Returns(gemAxisLength);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.LateralAxis_mm).Returns(lateralAxisLength);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionNorthSouth_mm).Returns(mountOffsetX);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionEastWest_mm).Returns(mountOffsetY);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionUpDown_mm).Returns(mountOffsetZ);
            this.siteLatitude = siteLatitude;
            this.siteLongitude = siteLongitude;
            this.localSiderealTime = AstroUtil.GetLocalSiderealTime(DateTime.Now, this.siteLongitude, this.db);
            var mountOffset = new Vector3D(mountOffsetX, mountOffsetY, mountOffsetZ);
            return new DomeSynchronization(mockProfileService.Object);
        }

        private Angle CalculateAzimuth(IDomeSynchronization domeSynchronization, Coordinates coordinates, PierSide sideOfPier) {
            return domeSynchronization.TargetDomeAzimuth(coordinates, localSiderealTime, Angle.ByDegree(siteLatitude), Angle.ByDegree(siteLongitude), sideOfPier);
        }

        private Coordinates GetCoordinatesFromAltAz(double altitude, double azimuth) {
            return new TopocentricCoordinates(
                azimuth: Angle.ByDegree(azimuth),
                altitude: Angle.ByDegree(altitude),
                latitude: Angle.ByDegree(siteLatitude),
                longitude: Angle.ByDegree(siteLongitude)).Transform(Epoch.JNOW, db);
        }

        [Test]
        public void Meridian_AltAz_Test() {
            var sut = Initialize(gemAxisLength: 0);
            // An AltAz (0-length GEM axis) pointed at the meridian should result in a dome perfectly centered at 0, regardless of the side of pier
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 0);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            Assert.IsTrue(westResult.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
        }

        [Test]
        public void Meridian_AltAz_SouthernHemisphere_Test() {
            var sut = Initialize(gemAxisLength: 0, siteLatitude: -41.3);
            // An AltAz (0-length GEM axis) pointed at the meridian should result in a dome perfectly centered at 0, regardless of the side of pier
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 180.0);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(Angle.ByDegree(180.0), DEGREES_EPSILON));
            Assert.IsTrue(westResult.Equals(Angle.ByDegree(180.0), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void Meridian_EQ_Test(double length) {
            var sut = Initialize(gemAxisLength: length);
            // When pointed at the meridian, a meridian flip when the EQ mount is perfectly centered should have the same absolute distance from 0
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 0);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(-1.0 * westResult, DEGREES_EPSILON));
            Assert.IsTrue(eastResult.Degree >= 0 && eastResult.Degree <= 90);
        }

        [Test]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void Meridian_EQ_SouthernHemisphere_Test(double length) {
            var sut = Initialize(gemAxisLength: length);
            // When pointed at the meridian, a meridian flip when the EQ mount is perfectly centered should have the same absolute distance from 0
            var coordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude) + 10.0, 180);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(-1.0 * westResult, DEGREES_EPSILON));
            Assert.IsTrue(eastResult.Degree >= 90 && eastResult.Degree <= 180);
        }

        [Test]
        [TestCase(15)]
        [TestCase(-15)]
        [TestCase(35)]
        [TestCase(-40)]
        [TestCase(80)]
        [TestCase(90)]
        [TestCase(-90)]
        public void CelestialEquator_AltAz_Test(double azimuth) {
            // On the celestial equator, the dome aziumth should be the same as the Alt-Az mount azimuth
            var sut = Initialize(gemAxisLength: 0);
            var coordinates = GetCoordinatesFromAltAz(0, azimuth);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
            Assert.IsTrue(westResult.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(15)]
        [TestCase(-15)]
        [TestCase(35)]
        [TestCase(-40)]
        [TestCase(80)]
        [TestCase(90)]
        [TestCase(-90)]
        public void CelestialEquator_AltAz_SouthernHemisphere_Test(double azimuth) {
            // On the celestial equator, the dome aziumth should be the same as the Alt-Az mount azimuth
            var sut = Initialize(gemAxisLength: 0, siteLatitude: -41.3);
            var coordinates = GetCoordinatesFromAltAz(0, azimuth);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
            Assert.IsTrue(westResult.Equals(Angle.ByDegree(azimuth), DEGREES_EPSILON));
        }

        [Test]
        public void NorthOffset_AltAz_Test() {
            var sut = Initialize(gemAxisLength: 0, mountOffsetX: 500, domeRadius: 1000);

            // When pointed to the east or west along the celestial equator, we expect the dome azimuth to be +/- 60 degrees, since the mount offset is half of the dome radius
            var eastCoordinates = GetCoordinatesFromAltAz(0, 90);
            Assert.IsTrue(CalculateAzimuth(sut, eastCoordinates, PierSide.pierEast).Equals(Angle.ByDegree(60.0), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, eastCoordinates, PierSide.pierWest).Equals(Angle.ByDegree(60.0), DEGREES_EPSILON));

            var westCoordinates = GetCoordinatesFromAltAz(0, -90);
            Assert.IsTrue(CalculateAzimuth(sut, westCoordinates, PierSide.pierEast).Equals(Angle.ByDegree(-60.0), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, westCoordinates, PierSide.pierWest).Equals(Angle.ByDegree(-60.0), DEGREES_EPSILON));
        }

        [Test]
        public void LateralOffset_CelestialPole_Test() {
            var domeRadius = 1000;
            var lateralAxisLength = 500;
            var sut = Initialize(lateralAxisLength: lateralAxisLength, domeRadius: domeRadius);

            // When pointed where the horizon and meridian intersect, we expect the dome azimuth to be +/- 60 degrees, since the lateral offset is half of the dome radius
            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(siteLatitude), 0);

            var distanceFromScopeOrigin = Math.Sqrt(domeRadius * domeRadius - lateralAxisLength * lateralAxisLength);
            var northProjectionDistanceToDomeIntersection = distanceFromScopeOrigin * Math.Cos(Angle.ByDegree(this.siteLatitude).Radians);
            var expectedAzimuth = Math.Atan(lateralAxisLength / northProjectionDistanceToDomeIntersection);

            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierEast).Equals(Angle.ByRadians(expectedAzimuth), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierWest).Equals(Angle.ByRadians(-expectedAzimuth), DEGREES_EPSILON));
        }

        [Test]
        public void LateralOffset_CelestialPole_SouthernHemisphere_Test() {
            var domeRadius = 1000;
            var lateralAxisLength = 500;
            var sut = Initialize(lateralAxisLength: lateralAxisLength, domeRadius: domeRadius, siteLatitude: -41.3);

            // When pointed where the horizon and meridian intersect, we expect the dome azimuth to be +/- 60 degrees, since the lateral offset is half of the dome radius
            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(this.siteLatitude), 180);

            var distanceFromScopeOrigin = Math.Sqrt(domeRadius * domeRadius - lateralAxisLength * lateralAxisLength);
            var southProjectionDistanceToDomeIntersection = distanceFromScopeOrigin * Math.Cos(Angle.ByDegree(this.siteLatitude).Radians);
            var expectedAzimuth = Math.Atan(lateralAxisLength / southProjectionDistanceToDomeIntersection);

            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierEast).Equals(Angle.ByRadians(Math.PI + expectedAzimuth), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierWest).Equals(Angle.ByRadians(Math.PI - expectedAzimuth), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(0)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void NorthOffset_AltAz_Test(int length) {
            var sut = Initialize(gemAxisLength: length, mountOffsetX: 500, domeRadius: 1000);

            // When pointed at the celestial pole, an AltAz should still have an azimuth of 0 as long as the E/W mount offset is 0, regardless of gem length
            var poleCoordinates = GetCoordinatesFromAltAz(Math.Abs(this.siteLatitude), 0);
            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierEast).Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierWest).Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
        }
    }
}