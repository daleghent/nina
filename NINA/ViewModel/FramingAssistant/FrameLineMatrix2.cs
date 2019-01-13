﻿#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel.FramingAssistant {

    internal class FrameLineMatrix2 {
        private List<double> STEPSIZES = new List<double>() { 1, 2, 4, 8, 24 };

        public FrameLineMatrix2() {
            RAPoints = new List<FrameLine>();
            DecPoints = new List<FrameLine>();
        }

        private double resolution;
        private Dictionary<double, List<Coordinates>> raCoordinateMatrix = new Dictionary<double, List<Coordinates>>();
        private Dictionary<double, List<Coordinates>> decCoordinateMatrix = new Dictionary<double, List<Coordinates>>();

        private const double minDec = -80;
        private const double maxDec = 80;
        private const double minRA = 0;
        private const double maxRA = 0;

        private void GenerateRACoordinateMatrix(double raStep) {
            raCoordinateMatrix.Clear();
            for (double i = minDec; i <= maxDec; i += resolution) {
                for (double ra = 0; ra < 360; ra += raStep) {
                    var coordinate = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(i), Epoch.J2000);
                    if (!raCoordinateMatrix.ContainsKey(ra)) {
                        raCoordinateMatrix[ra] = new List<Coordinates>();
                    }
                    raCoordinateMatrix[ra].Add(coordinate);
                }
            }
        }

        private void GenerateDecCoordinateMatrix(double decStep) {
            decCoordinateMatrix.Clear();

            for (double i = 0; i < 360; i += resolution) {
                for (double dec = minDec; dec <= maxDec; dec += decStep) {
                    var coordinate = new Coordinates(Angle.ByDegree(i), Angle.ByDegree(dec), Epoch.J2000);
                    if (!decCoordinateMatrix.ContainsKey(dec)) {
                        decCoordinateMatrix[dec] = new List<Coordinates>();
                    }
                    decCoordinateMatrix[dec].Add(coordinate);
                }
            }
        }

        private void DetermineStepSizes(ViewportFoV viewport) {
            var decStep = viewport.VFoVDeg / 4d;
            decStep = STEPSIZES.Aggregate((x, y) => Math.Abs(x - decStep) < Math.Abs(y - decStep) ? x : y);

            var raStep = viewport.HFoVDeg / 4d;
            raStep = STEPSIZES.Aggregate((x, y) => Math.Abs(x - raStep) < Math.Abs(y - raStep) ? x : y);

            resolution = Math.Min(raStep, decStep) / 3;

            if (currentRAStep != raStep) {
                currentRAStep = raStep;
                GenerateRACoordinateMatrix(raStep);
            }
            if (currentDecStep != decStep) {
                currentDecStep = decStep;
                GenerateDecCoordinateMatrix(decStep);
            }
        }

        private double currentRAStep;
        private double currentDecStep;

        public void CalculatePoints(ViewportFoV viewport) {
            DetermineStepSizes(viewport);

            RAPoints.Clear();
            DecPoints.Clear();

            //vertical
            for (double ra = 0; ra < 360; ra += currentRAStep) {
                var list = new List<PointF>();
                foreach (var coordinate in raCoordinateMatrix[ra]) {
                    if (viewport.ContainsCoordinates(coordinate)) {
                        var p = coordinate.GnomonicTanProjection(viewport);
                        var pointF = new PointF((float)p.X, (float)p.Y);

                        list.Add(pointF);
                    }
                }
                RAPoints.Add(new FrameLine() { Collection = list, StrokeThickness = 1, Closed = false });
            }

            for (double dec = minDec; dec <= maxDec; dec += currentDecStep) {
                double? prevRA = null;
                double? stepRA = null;
                var circled = false;
                var inViewDegreeSum = 0d;
                var degreeSum = 0d;
                var list = new LinkedList<PointF>();

                LinkedListNode<PointF> node = null;
                foreach (var coordinate in decCoordinateMatrix[dec]) {
                    degreeSum += coordinate.RADegrees;
                    if (viewport.ContainsCoordinates(coordinate)) {
                        inViewDegreeSum += coordinate.RADegrees;

                        if (stepRA == null && prevRA != null) {
                            stepRA = Math.Round((prevRA.Value - coordinate.RADegrees), 5);
                        }

                        var p = coordinate.GnomonicTanProjection(viewport);
                        var pointF = new PointF((float)p.X, (float)p.Y);

                        if (prevRA != null && Math.Round((prevRA.Value - coordinate.RADegrees), 5) != stepRA) {
                            node = list.First;
                            circled = true;
                            node = list.AddBefore(node, pointF);
                        } else {
                            if (circled) {
                                node = list.AddAfter(node, pointF);
                            } else {
                                node = list.AddLast(pointF);
                            }
                        }
                        prevRA = coordinate.RADegrees;
                    }
                }
                DecPoints.Add(new FrameLine() { Collection = new List<PointF>(list), StrokeThickness = 1, Closed = inViewDegreeSum == degreeSum });
            }
        }

        public List<FrameLine> RAPoints { get; private set; }

        public List<FrameLine> DecPoints { get; private set; }

        private static System.Drawing.Pen gridPen = new System.Drawing.Pen(System.Drawing.Color.SteelBlue);

        public void Draw(Graphics g) {
            foreach (var frameLine in this.RAPoints) {
                DrawFrameLineCollection(g, frameLine);
            }

            foreach (var frameLine in this.DecPoints) {
                DrawFrameLineCollection(g, frameLine);
            }
        }

        private void DrawFrameLineCollection(Graphics g, FrameLine frameLine) {
            if (frameLine.Collection.Count > 1) {
                var points = CardinalSpline(frameLine.Collection, 0.5f, frameLine.Closed);

                if (frameLine.StrokeThickness != 1) {
                    var pen = new System.Drawing.Pen(gridPen.Color, frameLine.StrokeThickness);
                    g.DrawBeziers(pen, points.ToArray());
                } else {
                    g.DrawBeziers(gridPen, points.ToArray());
                }
            }
        }

        private static void CalcCurve(PointF[] pts, float tension, out PointF p1, out PointF p2) {
            float deltaX, deltaY;
            deltaX = pts[2].X - pts[0].X;
            deltaY = pts[2].Y - pts[0].Y;
            p1 = new PointF((pts[1].X - tension * deltaX), (pts[1].Y - tension * deltaY));
            p2 = new PointF((pts[1].X + tension * deltaX), (pts[1].Y + tension * deltaY));
        }

        private void CalcCurveEnd(PointF end, PointF adj, float tension, out PointF p1) {
            p1 = new PointF(((tension * (adj.X - end.X) + end.X)), ((tension * (adj.Y - end.Y) + end.Y)));
        }

        private List<PointF> CardinalSpline(List<PointF> pts, float t, bool closed) {
            int i, nrRetPts;
            PointF p1, p2;
            float tension = t * (1f / 3f); //we are calculating contolpoints.

            if (closed)
                nrRetPts = (pts.Count + 1) * 3 - 2;
            else
                nrRetPts = pts.Count * 3 - 2;

            PointF[] retPnt = new PointF[nrRetPts];
            for (i = 0; i < nrRetPts; i++)
                retPnt[i] = new PointF();

            if (!closed) {
                CalcCurveEnd(pts[0], pts[1], tension, out p1);
                retPnt[0] = pts[0];
                retPnt[1] = p1;
            }
            for (i = 0; i < pts.Count - (closed ? 1 : 2); i++) {
                CalcCurve(new PointF[] { pts[i], pts[i + 1], pts[(i + 2) % pts.Count] }, tension, out p1, out p2);
                retPnt[3 * i + 2] = p1;
                retPnt[3 * i + 3] = pts[i + 1];
                retPnt[3 * i + 4] = p2;
            }
            if (closed) {
                CalcCurve(new PointF[] { pts[pts.Count - 1], pts[0], pts[1] }, tension, out p1, out p2);
                retPnt[nrRetPts - 2] = p1;
                retPnt[0] = pts[0];
                retPnt[1] = p2;
                retPnt[nrRetPts - 1] = retPnt[0];
            } else {
                CalcCurveEnd(pts[pts.Count - 1], pts[pts.Count - 2], tension, out p1);
                retPnt[nrRetPts - 2] = p1;
                retPnt[nrRetPts - 1] = pts[pts.Count - 1];
            }
            return new List<PointF>(retPnt);
        }
    }
}