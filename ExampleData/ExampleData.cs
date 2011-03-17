/* Poly2Tri
 * Copyright (c) 2009-2010, Poly2Tri Contributors
 * http://code.google.com/p/poly2tri/
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice,
 *   this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 * * Neither the name of Poly2Tri nor the names of its contributors may be
 *   used to endorse or promote products derived from this software without specific
 *   prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Globalization;

namespace Poly2Tri
{
    public static class ExampleData
    {
        static readonly Dictionary<string, Polygon> DatCache = new Dictionary<string, Polygon>();
        static readonly Dictionary<string, PointSet> DatCachePointSet = new Dictionary<string, PointSet>();
        static readonly Dictionary<string, Image> ImageCache = new Dictionary<string, Image>();


        public static Polygon LoadDat(string filename, bool xflip, bool yflip, bool displayFlipX, bool displayFlipY, float rotateAngleInDegrees)
        {
            var points = new List<PolygonPoint>();
            int lineNum = 0;
            double precision = TriangulationPoint.kVertexCodeDefaultPrecision;
            bool skipLine = false;
            foreach (var line_ in File.ReadAllLines(filename))
            {
                ++lineNum;
                string line = line_.Trim();
                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith("//") ||
                    line.StartsWith("#") ||
                    line.StartsWith(";"))
                {
                    continue;
                }
                if (!skipLine && line.StartsWith("/*"))
                {
                    skipLine = true;
                    continue;
                }
                else if (skipLine)
                {
                    if (line.StartsWith("*/"))
                    {
                        skipLine = false;
                    }
                    continue;
                }
                if (line.StartsWith("Precision", StringComparison.InvariantCultureIgnoreCase))
                {
                    if( line.Length> 9)
                    {
                        string ps = line.Substring(9).Trim();
                        if (!double.TryParse(ps, NumberStyles.Float, CultureInfo.InvariantCulture, out precision))
                        {
                            Console.WriteLine("Invalid Precision '" + ps + "' in file " + filename + ", line " + lineNum.ToString() +".  Setting to " + TriangulationPoint.kVertexCodeDefaultPrecision.ToString());
                            precision = TriangulationPoint.kVertexCodeDefaultPrecision;
                        }
                    }
                }
                else
                {
                    var xy = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    double x, y;
                    if (xy != null &&
                        xy.Length >= 2 &&
                        double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                        double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    {
                        points.Add(new PolygonPoint((xflip ? -1.0 : 1.0) * x, (yflip ? -1.0 : 1.0) * y));
                    }
                    else
                    {
                        Console.WriteLine("Invalid Input '" + line + "' in file " + filename + ", line " + lineNum.ToString());
                    }
                }
            }

            Polygon p = new Polygon(points);
            p.FileName = filename;
            p.DisplayFlipX = displayFlipX;
            p.DisplayFlipY = displayFlipY;
            p.DisplayRotate = rotateAngleInDegrees;
            p.Precision = precision;

            return p;
        }

        public static Polygon LoadDat(string filename) { return LoadDat(filename, false, false, false, false, 0.0f); }

        static Polygon CacheLoadDat(string filename, bool xflip, bool yflip, bool displayFlipX, bool displayFlipY, float rotateAngleInDegrees)
        {
            if (!DatCache.ContainsKey(filename))
            {
                DatCache.Add(filename, LoadDat(filename, xflip, yflip, displayFlipX, displayFlipY, rotateAngleInDegrees));
            }
            return DatCache[filename];
        }

        static Polygon CacheLoadDat(string filename) { return CacheLoadDat(filename, false, false, false, false, 0.0f); }


        ////////


        public static PointSet LoadDatPointSet(string filename, bool xflip, bool yflip, bool displayFlipX, bool displayFlipY, float rotateAngleInDegrees)
        {
            var points = new List<TriangulationPoint>();
            List<List<TriangulationPoint>> constrainedPoints = new List<List<TriangulationPoint>>();
            List<string> constrainedPointSetNames = new List<string>();
            List<TriangulationPoint> bounds = new List<TriangulationPoint>();
            List<TriangulationPoint> currentList = points;
            double precision = TriangulationPoint.kVertexCodeDefaultPrecision;
            int lineNum = 0;
            bool skipLine = false;
            foreach (var line_ in File.ReadAllLines(filename))
            {
                ++lineNum;
                string line = line_.Trim();
                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith("//") ||
                    line.StartsWith("#") ||
                    line.StartsWith(";"))
                {
                    continue;
                }
                if (!skipLine && line.StartsWith("/*"))
                {
                    skipLine = true;
                    continue;
                }
                else if (skipLine)
                {
                    if( line.StartsWith("*/"))
                    {
                        skipLine = false;
                    }
                    continue;
                }
                if (line.StartsWith("Precision", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (line.Length > 9)
                    {
                        string precisionStr = line.Substring(9).Trim();
                        if (!double.TryParse(precisionStr, NumberStyles.Float, CultureInfo.InvariantCulture, out precision))
                        {
                            Console.WriteLine("Invalid Precision '" + precisionStr + "' in file " + filename + ", line " + lineNum.ToString() + ".  Setting to " + TriangulationPoint.kVertexCodeDefaultPrecision.ToString());
                            precision = TriangulationPoint.kVertexCodeDefaultPrecision;
                        }
                    }
                }
                else if (line.StartsWith("Set", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (line.StartsWith("SetBegin"))
                    {
                        string[] setParts = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (setParts.Length < 2)
                        {
                            continue;
                        }
                        //if (setParts[1].Equals("Constrained", StringComparison.InvariantCultureIgnoreCase))
                        if (setParts[1].Equals("Constrained", StringComparison.InvariantCultureIgnoreCase) ||
                            setParts[1].Equals("Unconstrained", StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentList = new List<TriangulationPoint>();
                            constrainedPoints.Add(currentList);
                            if( setParts.Length>2)
                            {
                                constrainedPointSetNames.Add(setParts[2]);
                            }
                            else
                            {
                                constrainedPointSetNames.Add("");
                            }
                        }
                        else if (bounds.Count == 0 && setParts[1].Equals("Bounds", StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentList = bounds;
                        }
                        else
                        {
                            currentList = points;
                        }
                    }
                    else
                    {
                        currentList = points;
                    }
                }
                else
                {
                    var xy = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    double x, y;
                    if (xy != null &&
                        xy.Length >= 2 &&
                        double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                        double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    {
                        currentList.Add(new PolygonPoint((xflip ? -1.0 : 1.0) * x, (yflip ? -1.0 : 1.0) * y));
                    }
                    else
                    {
                        Console.WriteLine("Invalid Input '" + line + "' in file " + filename + ", line " + lineNum.ToString());
                    }
                }
            }

            PointSet ps = null;
            if (constrainedPoints.Count > 0)
            {
                ConstrainedPointSet cps = null;
                if (bounds.Count < 3)
                {
                    points.AddRange(bounds);
                    cps = new ConstrainedPointSet(points);
                }
                else
                {
                    cps = new ConstrainedPointSet(bounds);
                    cps.AddRange(points);
                }
                int numConstrainedPointSets = constrainedPoints.Count;
                for( int i = 0; i < numConstrainedPointSets; ++i)
                {
                    List<TriangulationPoint> hole = constrainedPoints[i];
                    if (hole.Count < 3)
                    {
                        cps.AddRange(hole);
                    }
                    else
                    {
                        cps.AddHole(hole, constrainedPointSetNames[i]);
                    }
                }
                ps = cps;
            }
            else
            {
                if (bounds.Count < 3)
                {
                    points.AddRange(bounds);
                    ps = new PointSet(points);
                }
                else
                {
                    ps = new PointSet(bounds);
                    ps.AddRange(points);
                }
            }

            ps.FileName = filename;
            ps.DisplayFlipX = displayFlipX;
            ps.DisplayFlipY = displayFlipY;
            ps.DisplayRotate = rotateAngleInDegrees;
            ps.Precision = precision;

            return ps;
        }

        public static PointSet LoadDatPointSet(string filename) { return LoadDatPointSet(filename, false, false, false, false, 0.0f); }

        static PointSet CacheLoadDatPointSet(string filename, bool xflip, bool yflip, bool displayFlipX, bool displayFlipY, float rotateAngleInDegrees)
        {
            if (!DatCache.ContainsKey(filename))
            {
                DatCachePointSet.Add(filename, LoadDatPointSet(filename, xflip, yflip, displayFlipX, displayFlipY, rotateAngleInDegrees));
            }
            return DatCachePointSet[filename];
        }

        static PointSet CacheLoadDatPointSet(string filename)
        {
            return CacheLoadDatPointSet(filename, false, false, false, false, 0.0f);
        }


        static Image CacheLoadImage(string filename)
        {
            if (!ImageCache.ContainsKey(filename))
            {
                ImageCache.Add(filename, new Bitmap(filename));
            }
            return ImageCache[filename];
        }

        // These should all use +x = right, +y = up
        public static Polygon Two { get { return CacheLoadDat(@"Data\2.dat", false, true, false, false, 0.0f); } }
        public static Polygon Bird { get { return CacheLoadDat(@"Data\bird.dat", false, false, false, false, 0.0f); } }
        public static Polygon Custom { get { return CacheLoadDat(@"Data\custom.dat"); } }
        public static Polygon Debug { get { return CacheLoadDat(@"Data\debug.dat"); } }
        public static Polygon Debug2 { get { return CacheLoadDat(@"Data\debug2.dat"); } }
        public static Polygon Diamond { get { return CacheLoadDat(@"Data\diamond.dat"); } }
        public static Polygon Dude
        {
            get
            {
                if (!ImageCache.ContainsKey(@"Data\dude.dat"))
                {
                    var p = CacheLoadDat(@"Data\dude.dat", false, false, false, true, 0.0f);
                    p.AddHole(new Polygon
                        (new PolygonPoint(325, 437)
                        , new PolygonPoint(320, 423)
                        , new PolygonPoint(329, 413)
                        , new PolygonPoint(332, 423)
                        ));
                    p.AddHole(new Polygon
                        (new PolygonPoint(320.72342, 480)
                        , new PolygonPoint(338.90617, 465.96863)
                        , new PolygonPoint(347.99754, 480.61584)
                        , new PolygonPoint(329.8148, 510.41534)
                        , new PolygonPoint(339.91632, 480.11077)
                        , new PolygonPoint(334.86556, 478.09046)
                        ));
                }
                return CacheLoadDat(@"Data\dude.dat");
            }
        }
        public static Polygon Funny { get { return CacheLoadDat(@"Data\funny.dat"); } }
        public static Polygon NazcaHeron { get { return CacheLoadDat(@"Data\nazca_heron.dat"); } }
        public static Polygon NazcaMonkey { get { return CacheLoadDat(@"Data\nazca_monkey.dat"); } }
        public static Polygon Sketchup { get { return CacheLoadDat(@"Data\sketchup.dat"); } }
        public static Polygon Star { get { return CacheLoadDat(@"Data\star.dat"); } }
        public static Polygon Strange { get { return CacheLoadDat(@"Data\strange.dat"); } }
        public static Polygon Tank { get { return CacheLoadDat(@"Data\tank.dat", false, false, false, false, 180.0f); } }
        public static Polygon Test { get { return CacheLoadDat(@"Data\test.dat"); } }

        public static IEnumerable<Polygon> Polygons
        {
            get
            {
                var l = new List<Polygon>();
                l.AddRange( new[] { Two, Bird, Custom, Debug, Debug2, Diamond, Dude, Funny, NazcaHeron, NazcaMonkey, Sketchup, Star, Strange, Tank, Test } );
                //l.AddRange(new[] { Bird });
                return l;
            }
        }

        public static PointSet NavMesh { get { return CacheLoadDatPointSet(@"Data\Pointsets\NavMeshPoints.dat", false, false, false, true, -90.0f); } }
        public static PointSet NavMeshTest { get { return CacheLoadDatPointSet(@"Data\Pointsets\NavMeshTest.dat", false, false, false, true, -90.0f); } }
        public static PointSet Example1 { get { return CacheLoadDatPointSet(@"Data\Pointsets\example1.dat", false, false, false, false, -90.0f); } }
        public static PointSet Example2 { get { return CacheLoadDatPointSet(@"Data\Pointsets\example2.dat"); } }
        public static PointSet Example3 { get { return CacheLoadDatPointSet(@"Data\Pointsets\example3.dat"); } }
        public static PointSet Example4 { get { return CacheLoadDatPointSet(@"Data\Pointsets\example4.dat"); } }

        public static IEnumerable<PointSet> PointSets
        {
            get
            {
                var l = new List<PointSet>();
                l.AddRange(new[] { NavMesh, NavMeshTest, Example1, Example2, Example3, Example4 });
                //l.AddRange(new[] { NavMesh });
                return l;
            }
        }

        public static Image Logo256x256 { get { return CacheLoadImage(@"Textures\poly2tri_logotype_256x256.png"); } }
    }
}
