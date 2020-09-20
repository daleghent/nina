using NUnit.Framework;
using System;
using NINA.Utility;
using System.Windows.Media.Media3D;
using NINA.Utility.Astrometry;
using NINA.Model.MyTelescope;
using NINA.Database;
using System.IO;
using Moq;
using NINA.Profile;
using ASCOM.Astrometry.NOVASCOM;

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
            double mountOffsetX = 0.0,
            double mountOffsetY = 0.0,
            double mountOffsetZ = 0.0,
            double siteLatitude = 41.3,
            double siteLongitude = -74.4) {
            var mockProfileService = new Mock<IProfileService>();
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.DomeRadius_mm).Returns(domeRadius);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.GemAxis_mm).Returns(gemAxisLength);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionEastWest_mm).Returns(mountOffsetX);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionNorthSouth_mm).Returns(mountOffsetY);
            mockProfileService.SetupGet(x => x.ActiveProfile.DomeSettings.ScopePositionUpDown_mm).Returns(mountOffsetZ);
            this.siteLatitude = siteLatitude;
            this.siteLongitude = siteLongitude;
            this.localSiderealTime = Astrometry.GetLocalSiderealTime(DateTime.Now, this.siteLongitude, this.db);
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
            var coordinates = GetCoordinatesFromAltAz(siteLatitude + 10.0, 0);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            Assert.IsTrue(westResult.Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void Meridian_EQ_Test(double length) {
            var sut = Initialize(gemAxisLength: length);
            // When pointed at the meridian, a meridian flip when the EQ mount is perfectly centered should have the same absolute distance from 0
            var coordinates = GetCoordinatesFromAltAz(siteLatitude + 10.0, 0);
            var eastResult = CalculateAzimuth(sut, coordinates, PierSide.pierEast);
            var westResult = -1.0 * CalculateAzimuth(sut, coordinates, PierSide.pierWest);
            Assert.IsTrue(eastResult.Equals(westResult, DEGREES_EPSILON));
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
        public void NorthOffset_AltAz_Test() {
            var sut = Initialize(gemAxisLength: 0, mountOffsetY: 500, domeRadius: 1000);

            // When pointed to the east or west along the celestial equator, we expect the dome azimuth to be +/- 60 degrees, since the mount offset is half of the dome radius
            var eastCoordinates = GetCoordinatesFromAltAz(0, 90);
            Assert.IsTrue(CalculateAzimuth(sut, eastCoordinates, PierSide.pierEast).Equals(Angle.ByDegree(60.0), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, eastCoordinates, PierSide.pierWest).Equals(Angle.ByDegree(60.0), DEGREES_EPSILON));

            var westCoordinates = GetCoordinatesFromAltAz(0, -90);
            Assert.IsTrue(CalculateAzimuth(sut, westCoordinates, PierSide.pierEast).Equals(Angle.ByDegree(-60.0), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, westCoordinates, PierSide.pierWest).Equals(Angle.ByDegree(-60.0), DEGREES_EPSILON));
        }

        [Test]
        [TestCase(0)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(600)]
        public void NorthOffset_AltAz_Test(int length) {
            var sut = Initialize(gemAxisLength: length, mountOffsetY: 500, domeRadius: 1000);

            // When pointed at the celestial pole, an AltAz should still have an azimuth of 0 as long as the E/W mount offset is 0, regardless of gem length
            var poleCoordinates = GetCoordinatesFromAltAz(this.siteLatitude, 0);
            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierEast).Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
            Assert.IsTrue(CalculateAzimuth(sut, poleCoordinates, PierSide.pierWest).Equals(Angle.ByDegree(0.0), DEGREES_EPSILON));
        }
    }
}