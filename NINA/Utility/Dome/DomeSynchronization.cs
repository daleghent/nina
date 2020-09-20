using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Astrometry;
using System;
using System.Windows.Media.Media3D;

namespace NINA.Utility {
    public class DomeSynchronization : IDomeSynchronization {
        private static double TWO_PI = 2.0 * Math.PI;
        private static double HALF_PI = Math.PI / 2.0;

        private readonly IProfileService profileService;
        public DomeSynchronization(IProfileService profileService) {
            this.profileService = profileService;
        }

        /// <summary>
        /// Gets the dome azimuth required so the scope points directly out of the shutter. This works for Alt-Az and EQ mounts,
        /// and depends on careful user measurements including:
        ///  1) Dome radius, in mm
        ///  2) GEM axis length, in mm - starting from where the RA and DEC axes intersect, measured laterally to the center of the scope aperture
        ///     Alt-Az mounts set this value to 0
        ///  3) Mount offset as a 3D vector relative to the center of the dome sphere. The y-axis in the positive direction points towards true North
        ///  4) The latitude and longitude of the scope site
        /// 
        /// This method uses an algorithm that solves equations that are derived in the Wikipedia article
        /// entitled Line-sphere intersection (https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection)
        /// 
        /// Approach inspired by ASCOM Device Hub, but with some mathematical changes the author believes are more correct.
        /// https://github.com/ASCOMInitiative/ASCOMDeviceHub/blob/2696a5ff056f1132b5bb2eb6131e10fe275b3aa0/DeviceHub/Business%20Object%20Classes/Dome%20Classes/DomeSynchronize.cs
        /// </summary>
        /// <param name="scopeCoordinates">The scope coordinates to derive the dome azimuth from</param>
        /// <param name="localSiderealTime">The local sidereal time</param>
        /// <param name="siteLatitude">The site latitude</param>
        /// <param name="siteLongitude">The site longitude</param>
        /// <param name="sideOfPier">The side of pier. If this is unknown, this method will throw an exception</param>
        /// <returns>An angle representing the Dome Azimuth that lines up with the scope coordinates</returns>
        public Angle TargetDomeAzimuth(
            Coordinates scopeCoordinates,
            double localSiderealTime,
            Angle siteLatitude,
            Angle siteLongitude,
            PierSide sideOfPier) {
            if (sideOfPier == PierSide.pierUnknown) {
                throw new InvalidOperationException("Side of Pier is unknown");
            }

            var domeSettings = profileService.ActiveProfile.DomeSettings;
            var domeRadius = domeSettings.DomeRadius_mm;
            var gemAxisLength = domeSettings.GemAxis_mm;
            var mountOffset = new Vector3D(
                domeSettings.ScopePositionEastWest_mm,
                domeSettings.ScopePositionNorthSouth_mm,
                domeSettings.ScopePositionUpDown_mm);
            scopeCoordinates = scopeCoordinates.Transform(Epoch.JNOW);

            // Calculate a vector pointing from the origin (dome center) to the center of the scope aperture
            var localHour = Angle.ByHours(scopeCoordinates.RA - localSiderealTime);
            var topocentricCoordinates = scopeCoordinates.Transform(siteLatitude, siteLongitude);
            var altitudeRadians = topocentricCoordinates.Altitude.Radians;
            var azimuthRadians = topocentricCoordinates.Azimuth.Radians;
            var hourAngleRadians = localHour.Radians;
            var pierFactor = (sideOfPier == PierSide.pierEast) ? 1.0 : -1.0;
            // The positive y-axis points true North, and theta (in 3D polar coordinate space) indicates the angle along the Z-plane from the positive x-axis
            // hourAngleRadians represents the local hour distance (in radians) from true North. A meridian flip inverts the origin
            var scopeApertureOrigin = Astrometry.Astrometry.Polar3DToCartesian(gemAxisLength, hourAngleRadians, siteLatitude.Radians) * pierFactor + mountOffset;

            // Calculate the pointing direction of the scope as a unit vector
            var alt = HALF_PI - altitudeRadians;
            var az = HALF_PI - azimuthRadians;
            var xSlope = Math.Sin(alt) * Math.Cos(az);
            var ySlope = Math.Sin(alt) * Math.Sin(az);
            var zSlope = Math.Cos(alt);
            var scopeDirection = new Vector3D(xSlope, ySlope, zSlope);
            scopeDirection.Normalize();

            // Calculate the distance along the unit vector, originating from the scope aperture origin, to where the line
            // intersects the sphere
            var dotProduct = Vector3D.DotProduct(scopeDirection, scopeApertureOrigin);
            var underRoot = (dotProduct * dotProduct) - scopeApertureOrigin.LengthSquared + (domeRadius * domeRadius);
            var distance = -dotProduct + Math.Sqrt(underRoot);

            // Calculate the intersection point with the sphere
            var intersection = scopeApertureOrigin + scopeDirection * distance;
            
            // Finally, calculate the azimuth of that intersection point, and ensure it is within [0, 2PI)
            // Similar trigonometry can get the altitude, but we don't need it at this time
            var domeAzimuthRadians = (Math.Atan2(intersection.X, intersection.Y) + TWO_PI) % TWO_PI;
            return Angle.ByRadians(domeAzimuthRadians);
        }
    }
}
