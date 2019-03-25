using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace LandscapeBuilderLib
{
    class TerrainFlattener
    {
        private PointF[] _cornersLatLong;
        private Point[] _cornersLandscapeXY;
        private PointF[] _cornersLandscapeXYUnrounded;
        private short _elevation;

        private Point[] _borderPoints;
        private Point[] _innerPoints;

        public TerrainFlattener(PointF[] cornersLatLong, short elevation)
        {
            _cornersLatLong = cornersLatLong;
            _elevation = elevation;
            _cornersLandscapeXY = getCornersLandscapeXY(_cornersLatLong);
            _borderPoints = getBounds(_cornersLandscapeXY);
            _innerPoints = getInnerPoints(_borderPoints);
        }

        public void Flatten(bool outputToCondor = false)
        {
            Dictionary<string, byte[]> heightmapFiles = new Dictionary<string, byte[]>();

            int width = SettingsManager.PatchHeightMeters / SettingsManager.HeightMapResolution + 1;
            foreach (Point point in _borderPoints.Union(_innerPoints))
            {
                string patchName = Utilities.GetPatchNameFromLandscapeCoordinates(point);
                PointF patchCoordinates = Utilities.GetPatchCoordinatesFromLandscapeCoordinates(point);
                int xOffset = (int)patchCoordinates.X / SettingsManager.HeightMapResolution;
                int yOffset = (int)patchCoordinates.Y / SettingsManager.HeightMapResolution;

                // If this height map file has not already been loaded, then load it from the landscape.
                if (!heightmapFiles.ContainsKey(patchName))
                { 
                    string patchPath = Path.Combine(SettingsManager.Instance.CondorLandscape, string.Format(@"{0}\Heightmaps\h{1}.tr3", SettingsManager.Instance.LandscapeName, patchName));
                    heightmapFiles.Add(patchName, File.ReadAllBytes(patchPath));
                }

                byte[] elevationBytes = BitConverter.GetBytes(_elevation);
                int offset = (xOffset * width * elevationBytes.Length) + (yOffset * elevationBytes.Length);
                Array.Copy(elevationBytes, 0, heightmapFiles[patchName], offset, elevationBytes.Length);
            }

            // Now write out all of the updated files.
            foreach(var heightmap in heightmapFiles)
            {
                string patchFile = string.Format("h{0}.tr3", heightmap.Key);
                File.WriteAllBytes(Path.Combine(SettingsManager.Instance.OutputHeightMap, patchFile), heightmap.Value);
            }
        }

        // Convert the corners from lat/long to the landscape's XY coordinates, rounded to the nearest valid value for the heightmap resolution
        private Point[] getCornersLandscapeXY(PointF[] cornersLatLong)
        {
            Point[] cornersLandscapeXY = new Point[cornersLatLong.Length];
            PointF[] cornersLandscapeXYUnrounded = new PointF[cornersLatLong.Length];

            bool missingCoCoCo = false;
            for (int i = 0; i < cornersLandscapeXY.Length; i++)
            {
                // TODO: This is assuming that 0 is TL, 1 is TR, 2 is BR and 3 is BL. Need to confirm this is true for all cases.
                // Convert lat/long to the landscape's XY coordinates.
                PointF landscapeXY = Utilities.LatLongToLandscapeXY(cornersLatLong[i], ref missingCoCoCo);

                if (missingCoCoCo)
                {
                    break;
                }

                cornersLandscapeXYUnrounded[i] = landscapeXY;
            }

            // Store in the member variable for display purposes.
            _cornersLandscapeXYUnrounded = cornersLandscapeXYUnrounded;

            // Round outwards to the nearest value divisible by the heightmap's resolution.
            PointF topLeft = cornersLandscapeXYUnrounded[(int)RunwayCorner.TopLeft];
            PointF bottomLeft = cornersLandscapeXYUnrounded[(int)RunwayCorner.BottomLeft];
            PointF bottomRight = cornersLandscapeXYUnrounded[(int)RunwayCorner.BottomRight];
            PointF topRight = cornersLandscapeXYUnrounded[(int)RunwayCorner.TopRight];

            // The slope for the left and right sides here will be the same, since the values haven't been rounded yet.
            double slope = Utilities.GetSlope(topLeft, bottomLeft);

            for (int i = 0; i < cornersLandscapeXYUnrounded.Length; i++)
            {
                PointF landscapeXY = cornersLandscapeXYUnrounded[i];

                int x = 0;
                int y = 0;
                switch ((RunwayCorner)i)
                {
                    // For Y, just round out to the nearest point on the grid of heightmap points. 
                    // For X, use the line equation to make sure that the point we round to is outside of the lines of the runway.
                    case RunwayCorner.TopLeft:
                        {
                            y = roundUpToHeightMapRes(landscapeXY.Y);
                            x = roundUpToHeightMapRes(((y - topLeft.Y) / slope) + topLeft.X);
                        }
                        break;
                    case RunwayCorner.TopRight:
                        {
                            y = roundUpToHeightMapRes(landscapeXY.Y);
                            x = roundDownToHeightMapRes(((y - topRight.Y) / slope) + topRight.X);
                        }
                        break;
                    case RunwayCorner.BottomRight:
                        {
                            y = roundDownToHeightMapRes(landscapeXY.Y);
                            x = roundDownToHeightMapRes(((y - bottomRight.Y) / slope) + bottomRight.X);
                        }
                        break;
                    case RunwayCorner.BottomLeft:
                        {
                            y = roundDownToHeightMapRes(landscapeXY.Y);
                            x = roundUpToHeightMapRes(((y - bottomLeft.Y) / slope) + bottomLeft.X);
                        }
                        break;
                }

                cornersLandscapeXY[i] = new Point(x, y);
            }

            return cornersLandscapeXY;
        }

        private Point[] getBounds(Point[] cornersLandscapeXY)
        {
            // This is the list of point that make up the outline of the region that needs to be flattened.
            List<Point> boundingPoints = new List<Point>();

            Point topLeft = cornersLandscapeXY[(int)RunwayCorner.TopLeft];
            Point bottomLeft = cornersLandscapeXY[(int)RunwayCorner.BottomLeft];
            Point bottomRight = cornersLandscapeXY[(int)RunwayCorner.BottomRight];
            Point topRight = cornersLandscapeXY[(int)RunwayCorner.TopRight]; 

            // Calculate the slopes
            double leftSlope = Utilities.GetSlope(topLeft, bottomLeft);
            double bottomSlope = Utilities.GetSlope(bottomLeft, bottomRight);
            double rightSlope = Utilities.GetSlope(bottomRight, topRight);
            double topSlope = Utilities.GetSlope(topRight, topLeft);

            int x2, y2;
            // First add the top left point.
            boundingPoints.Add(topLeft);

            // Then add the points along the left side of the runway.
            y2 = topLeft.Y;
            while (y2 > bottomLeft.Y)
            {
                // Go to the next Y intercept and figure out the X coordinate
                y2 -= SettingsManager.HeightMapResolution;
                x2 = roundUpToHeightMapRes(((y2 - topLeft.Y) / leftSlope) + topLeft.X);
                boundingPoints.Add(new Point(x2, y2));
            }

            // Then add the bottom left point.
            boundingPoints.Add(bottomLeft);

            // Then add the points along the bottom of the runway.
            x2 = bottomLeft.X;
            while (x2 > bottomRight.X)
            {
                x2 -= SettingsManager.HeightMapResolution;
                y2 = roundDownToHeightMapRes((x2 - bottomLeft.X) * bottomSlope + bottomLeft.Y);
                boundingPoints.Add(new Point(x2, y2));
            }

            // Then add the bottom right point.
            boundingPoints.Add(bottomRight);

            // Then add the points along the right side of the runway.
            y2 = bottomRight.Y;
            while (y2 < topRight.Y)
            {
                y2 += SettingsManager.HeightMapResolution;
                x2 = roundDownToHeightMapRes(((y2 - bottomRight.Y) / rightSlope) + bottomRight.X);
                boundingPoints.Add(new Point(x2, y2));
            }

            // Then add the top right point
            boundingPoints.Add(topRight);

            // Then add the points along the top side of the runway.
            x2 = topRight.X;
            while (x2 < topLeft.X)
            {
                x2 += SettingsManager.HeightMapResolution;
                y2 = roundUpToHeightMapRes((x2 - topRight.X) * topSlope + topRight.Y);
                boundingPoints.Add(new Point(x2, y2));
            }

            return boundingPoints.ToArray();
        }

       
        private Point[] getInnerPoints(Point[] bounds)
        {      
            List<Point> innerPoints = new List<Point>();

            // Get min and max values from graphics path.
            int xMin = int.MaxValue;
            int yMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMax = int.MinValue;
            foreach(Point point in bounds)
            {
                if(point.X < xMin)
                {
                    xMin = point.X;
                }

                if(point.Y < yMin)
                {
                    yMin = point.Y;
                }

                if(point.X > xMax)
                {
                    xMax = point.X;
                }

                if(point.Y > yMax)
                {
                    yMax = point.Y;
                }
            }

            GraphicsPath graphicsPath = new GraphicsPath();
            graphicsPath.AddPolygon(bounds);

            // Brute force it, and go through all of the possible values from the min and max and check to see if they are inside the region.
            for(int i = xMax; i > xMin; i -= SettingsManager.HeightMapResolution)
            {
                for(int j = yMax; j > yMin; j -= SettingsManager.HeightMapResolution)
                {
                    Point point = new Point(i, j);
                    // For some reason this is considering points along one of the sides of the bounds to be within the path.
                    // We'll be combining the inner points with the outer at the end though, so I guess it doesn't really matter.
                    if(graphicsPath.IsVisible(point))
                    {
                        innerPoints.Add(point);
                    }
                }
            }

            return innerPoints.ToArray();
        }

        // Rounds up to the nearest X or Y coordinate for the heightmap resolution.
        private int roundUpToHeightMapRes(double val)
        {
            return (int)Math.Ceiling(val / SettingsManager.HeightMapResolution) * SettingsManager.HeightMapResolution;
        }

        // Rounds down to the nearest X or Y coordinate for the heightmap resolution.
        private int roundDownToHeightMapRes(double val)
        {
            return (int)Math.Floor(val / SettingsManager.HeightMapResolution) * SettingsManager.HeightMapResolution;
        }

        // Outputs the points to flatten in a manner easy to plot on desmos.com.
        public List<string> ToStringList()
        {
            List<string> strings = new List<string>();

            string unroundedCorners = string.Empty;
            foreach (PointF point in _cornersLandscapeXYUnrounded)
            {
                unroundedCorners += string.Format("-{0},{1}\n", point.X, point.Y);

                // Add the first point to close the shape.
                if (point == _cornersLandscapeXYUnrounded.Last())
                {
                    unroundedCorners += string.Format("-{0},{1}\n", _cornersLandscapeXYUnrounded.First().X, _cornersLandscapeXYUnrounded.First().Y);
                }
            }
            strings.Add(unroundedCorners);

            string roundedCorners = string.Empty;
            foreach (Point point in _cornersLandscapeXY)
            {
                roundedCorners += string.Format("-{0},{1}\n", point.X, point.Y);

                if (point == _cornersLandscapeXY.Last())
                {
                    roundedCorners += string.Format("-{0},{1}\n", _cornersLandscapeXY.First().X, _cornersLandscapeXY.First().Y);
                }
            }
            strings.Add(roundedCorners);

            string bounds = string.Empty;
            foreach (Point point in _borderPoints)
            {
                bounds += string.Format("-{0},{1}\n", point.X, point.Y);

                if (point == _borderPoints.Last())
                {
                    bounds += string.Format("-{0},{1}\n", _borderPoints.First().X, _borderPoints.First().Y);
                }
            }
            strings.Add(bounds);

            string inner = string.Empty;
            foreach (Point point in _innerPoints)
            {
                inner += string.Format("-{0},{1}\n", point.X, point.Y);
            }
            strings.Add(inner);

            string union = string.Empty;
            foreach (Point point in _borderPoints.Union(_innerPoints))
            {
                union += string.Format("-{0},{1}\n", point.X, point.Y);
            }
            strings.Add(union);

            return strings;
        }
    }
}
