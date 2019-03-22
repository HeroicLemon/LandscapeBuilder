using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace LandscapeBuilderLib
{
    class TerrainFlattener
    {

        private const int heightmapResolution = 30;
        private PointF[] _cornersLatLong;
        private Point[] _cornersLandscapeXY;
        private PointF[] _cornersLandscapeXYUnrounded;
        private float _elevation;

        private PointF[] _boundingPoints;

        public TerrainFlattener(PointF[] cornersLatLong, float elevation)
        {
            _cornersLatLong = cornersLatLong;
            _elevation = elevation;
            _cornersLandscapeXY = getCornersLandscapeXY(_cornersLatLong);
            _boundingPoints = getBoundingPoints(_cornersLandscapeXY);
            Point centerPoint = getCenterPoint(_cornersLandscapeXY);
        }

        // Outputs the points to flatten in a manner easy to plot on desmos.com.
        public List<string> ToStringList()
        {
            List<string> strings = new List<string>();

            //// Left line
            //strings.Add(@"y-y_1=m\left(x-x_1\right)");
            //// Left slope
            //strings.Add(@"m=\frac{y_2-y_1}{x_2-x_1}");
            //// Top left point
            //strings.Add(@"\left(x_1,y_1\right)");
            //// Bottom left point
            //strings.Add(@"\left(x_2,y_2\right)");
            //// Right line
            //strings.Add(@"y-y_3=m_2\left(x-x_3\right)");
            //// Right slope
            //strings.Add(@"m_2=\frac{y_4-y_3}{x_4-x_3}");
            //// Top right point
            //strings.Add(@"\left(x_3,y_3\right)");
            //// Bottom right point
            //strings.Add(@"\left(x_4,y_4\right)");
            //// Top left X
            //strings.Add(string.Format(@"x_1=-{0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.TopLeft].X));
            //// Top left Y
            //strings.Add(string.Format(@"y_1={0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.TopLeft].Y));
            //// Bottom left X
            //strings.Add(string.Format(@"x_2=-{0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.BottomLeft].X));
            //// Bottom left Y
            //strings.Add(string.Format(@"y_2={0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.BottomLeft].Y));
            //// Top right X
            //strings.Add(string.Format(@"x_3=-{0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.TopRight].X));
            //// Top right Y
            //strings.Add(string.Format(@"y_3={0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.TopRight].Y));
            //// Bottom right X
            //strings.Add(string.Format(@"x_4=-{0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.BottomRight].X));
            //// Bottom right Y
            //strings.Add(string.Format(@"y_4={0}", _cornersLandscapeXYUnrounded[(int)RunwayCorner.BottomRight].Y));

            string unroundedCorners = string.Empty;
            foreach(PointF point in _cornersLandscapeXYUnrounded)
            {
                unroundedCorners += string.Format("-{0},{1}\n", point.X, point.Y);
                
                // Add the first point to close the shape.
                if(point == _cornersLandscapeXYUnrounded.Last())
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
            foreach(PointF point in _boundingPoints)
            {
                bounds += string.Format("-{0},{1}\n", point.X, point.Y);

                if (point == _boundingPoints.Last())
                {
                    bounds += string.Format("-{0},{1}\n", _boundingPoints.First().X, _boundingPoints.First().Y);
                }
            }
            strings.Add(bounds);


            return strings;
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

        private PointF[] getBoundingPoints(Point[] cornersLandscapeXY)
        {
            // This is the list of point that make up the outline of the region that needs to be flattened.
            List<PointF> boundingPoints = new List<PointF>();

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
                y2 -= heightmapResolution;
                x2 = roundUpToHeightMapRes(((y2 - topLeft.Y) / leftSlope) + topLeft.X);
                boundingPoints.Add(new PointF(x2, y2));
            }

            // Then add the bottom left point.
            boundingPoints.Add(bottomLeft);

            // Then add the points along the bottom of the runway.
            x2 = bottomLeft.X;
            while (x2 > bottomRight.X)
            {
                x2 -= heightmapResolution;
                y2 = roundDownToHeightMapRes((x2 - bottomLeft.X) * bottomSlope + bottomLeft.Y);
                boundingPoints.Add(new PointF(x2, y2));
            }

            // Then add the bottom right point.
            boundingPoints.Add(bottomRight);

            // Then add the points along the right side of the runway.
            y2 = bottomRight.Y;
            while (y2 < topRight.Y)
            {
                y2 += heightmapResolution;
                x2 = roundDownToHeightMapRes(((y2 - bottomRight.Y) / rightSlope) + bottomRight.X);
                boundingPoints.Add(new PointF(x2, y2));
            }

            // Then add the top right point
            boundingPoints.Add(topRight);

            // Then add the points along the top side of the runway.
            x2 = topRight.X;
            while (x2 < topLeft.X)
            {
                x2 += heightmapResolution;
                y2 = roundUpToHeightMapRes((x2 - topRight.X) * topSlope + topRight.Y);
                boundingPoints.Add(new PointF(x2, y2));
            }

            return boundingPoints.ToArray();
        }

        private Point getCenterPoint(Point[] roundedCorners)
        {
            Point center = new Point
            {
                X = (int)Math.Round(roundedCorners.Distinct().Average(p => p.X)),
                Y = (int)Math.Round(roundedCorners.Distinct().Average(p => p.Y))
            };

            return center;
        }

        // Rounds up to the nearest X or Y coordinate for the heightmap resolution.
        private int roundUpToHeightMapRes(double val)
        {
            return (int)Math.Ceiling(val / heightmapResolution) * heightmapResolution;
        }

        // Rounds down to the nearest X or Y coordinate for the heightmap resolution.
        private int roundDownToHeightMapRes(double val)
        {
            return (int)Math.Floor(val / heightmapResolution) * heightmapResolution;
        }
    }
}
