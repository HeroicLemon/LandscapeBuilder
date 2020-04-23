using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LandscapeBuilderLib
{
    public static class Utilities
    {
        [DllImport("LatLonXY64.dll")]
        public static extern bool xytolatlon_(out float x, out float y, out float lat, out float lon);
        [DllImport("LatLonXY64.dll")]
        public static extern bool latlontoxy_(out float lat, out float lon, out float x, out float y);
        [DllImport("LatLonXY64.dll")]
        public static extern bool utm_init_c_(string file, out float xmax, out float ymax);

        public static double GetSlope(PointF p1, PointF p2)
        {
            return (double)(p2.Y - p1.Y) / (double)(p2.X - p1.X);
        }

        // Uses CoCoCo to translate the lat/long corners into the landscape's XY coordinates.
        public static PointF LatLongToLandscapeXY(PointF latLong)
        {
            float xmax, ymax = 0;
            float lat = latLong.Y;
            float lon = latLong.X;
            float x = -1;
            float y = -1;

            string trnFile = string.Format("{0}.trn", SettingsManager.Instance.LandscapeName);
            string trnPath = Path.Combine(SettingsManager.Instance.CurrentLandscapeDir, trnFile);

            if (utm_init_c_(trnPath, out xmax, out ymax))
            {
                if (!latlontoxy_(out lat, out lon, out x, out y))
                {
                    Console.WriteLine("Conversion failed");
                }    
            }
            else
                Console.WriteLine("Conversion could not be initialized");

            return new PointF(x, y);
        }

        //// Uses CoCoCo to translate the lat/long corners into the landscape's XY coordinates.
        //public static PointF LatLongToLandscapeXY(PointF latLong, ref bool missingCoCoCo)
        //{
        //    PointF landscapeXY = new Point(-1, -1);

        //    Process process = new Process();
        //    process.StartInfo.FileName = "CoCoCo.exe";
        //    process.StartInfo.Arguments = string.Format("{0} {1} {2}", SettingsManager.Instance.LandscapeName, latLong.Y, latLong.X);
        //    process.StartInfo.UseShellExecute = false;
        //    process.StartInfo.RedirectStandardOutput = true;
        //    process.StartInfo.CreateNoWindow = true;

        //    // Make sure working directory is set up properly so that CoTaCo.ini can be parsed.
        //    if (!File.Exists(Path.Combine(SettingsManager.Instance.ExecutableDir, process.StartInfo.FileName)))
        //    {
        //        foreach (string path in Environment.GetEnvironmentVariable("PATH").Split(';'))
        //        {
        //            if (File.Exists(Path.Combine(path, process.StartInfo.FileName)))
        //            {
        //                process.StartInfo.WorkingDirectory = path;
        //                break;
        //            }
        //        }
        //    }

        //    try
        //    {
        //        process.Start();

        //        float posX = float.MinValue;
        //        float posY = float.MinValue;
        //        while (!process.StandardOutput.EndOfStream)
        //        {
        //            string line = process.StandardOutput.ReadLine();
        //            if (line.Contains("TPPosX"))
        //            {
        //                float.TryParse(line.Substring(line.IndexOf('=') + 1), out posX);
        //            }
        //            else if (line.Contains("TPPosY"))
        //            {
        //                float.TryParse(line.Substring(line.IndexOf('=') + 1), out posY);
        //            }
        //        }

        //        process.WaitForExit();


        //        if (posX > float.MinValue && posY > float.MinValue)
        //        {
        //            landscapeXY = new PointF(posX, posY);
        //        }
        //    }
        //    catch (System.ComponentModel.Win32Exception)
        //    {
        //        missingCoCoCo = true;
        //        //writeLine(string.Format("Failed to run {0}. Ensure {0} is in '{1}' or set up in the Path environment variable", process.StartInfo.FileName, SettingsManager.Instance.Executable));
        //    }

        //    return landscapeXY;
        //}

        // Should have been able to reproject using DotSpatial but I could not figure it out.
        // Implementation from https://www.ibm.com/developerworks/library/j-coordconvert/index.html
        public static PointF UTMToLatLong(float northing, float easting, int zone, char hemisphere)
        {
            // Ellipsoid parameters.
            int a = 6378137;
            double e = 0.081819191;
            double e1sq = 0.006739497;
            double k0 = 0.9996;

            if (hemisphere == 'S')
            {
                northing = 10000000 - northing;
            }

            double arc = northing / k0;
            double mu = arc / (a * (1 - Math.Pow(e, 2) / 4.0 - 3 * Math.Pow(e, 4) / 64.0 - 5 * Math.Pow(e, 6) / 256.0));

            double ei = (1 - Math.Pow((1 - e * e), (1 / 2.0))) / (1 + Math.Pow((1 - e * e), (1 / 2.0)));

            double ca = 3 * ei / 2 - 27 * Math.Pow(ei, 3) / 32.0;

            double cb = 21 * Math.Pow(ei, 2) / 16 - 55 * Math.Pow(ei, 4) / 32;
            double cc = 151 * Math.Pow(ei, 3) / 96;
            double cd = 1097 * Math.Pow(ei, 4) / 512;
            double phi1 = mu + ca * Math.Sin(2 * mu) + cb * Math.Sin(4 * mu) + cc * Math.Sin(6 * mu) + cd * Math.Sin(8 * mu);

            double n0 = a / Math.Pow((1 - Math.Pow((e * Math.Sin(phi1)), 2)), (1 / 2.0));

            double r0 = a * (1 - e * e) / Math.Pow((1 - Math.Pow((e * Math.Sin(phi1)), 2)), (3 / 2.0));
            double fact1 = n0 * Math.Tan(phi1) / r0;

            double _a1 = 500000 - easting;
            double dd0 = _a1 / (n0 * k0);
            double fact2 = dd0 * dd0 / 2;

            double t0 = Math.Pow(Math.Tan(phi1), 2);
            double Q0 = e1sq * Math.Pow(Math.Cos(phi1), 2);
            double fact3 = (5 + 3 * t0 + 10 * Q0 - 4 * Q0 * Q0 - 9 * e1sq) * Math.Pow(dd0, 4) / 24;

            double fact4 = (61 + 90 * t0 + 298 * Q0 + 45 * t0 * t0 - 252 * e1sq - 3 * Q0 * Q0) * Math.Pow(dd0, 6) / 720;

            double lof1 = _a1 / (n0 * k0);
            double lof2 = (1 + 2 * t0 + Q0) * Math.Pow(dd0, 3) / 6.0;
            double lof3 = (5 - 2 * Q0 + 28 * t0 - 3 * Math.Pow(Q0, 2) + 8 * e1sq + 24 * Math.Pow(t0, 2)) * Math.Pow(dd0, 5) / 120;
            double _a2 = (lof1 - lof2 + lof3) / Math.Cos(phi1);
            double _a3 = _a2 * 180 / Math.PI;


            double latitude = 180 * (phi1 - fact1 * (fact2 + fact3 + fact4)) / Math.PI;

            double zoneCM;
            if (zone > 0)
            {
                zoneCM = 6 * zone - 183.0;
            }
            else
            {
                zoneCM = 3.0;
            }

            double longitude = zoneCM - _a3;
            if (hemisphere == 'S')
            {
                latitude = -latitude;
            }

            return new PointF((float)longitude, (float)latitude);
        }

        public static Point GetTileCoordinatesFromName(string tileName)
        {
            int tileX = int.Parse(tileName.Substring(0, 2));
            int tileY = int.Parse(tileName.Substring(2, 2));

            return new Point(tileX, tileY);
        }

        public static string GetPatchNameFromLandscapeCoordinates(PointF point)
        {
            // Each patch is 5760 x 5760 m.
            point.X /= SettingsManager.PatchHeightMeters;
            point.Y /= SettingsManager.PatchHeightMeters;

            return string.Format("{0:00}{1:00}",Math.Floor(point.X), Math.Floor(point.Y));
        }

        public static PointF GetPatchCoordinatesFromLandscapeCoordinates(PointF point)
        {
            point.X = point.X % SettingsManager.PatchHeightMeters;
            point.Y = point.Y % SettingsManager.PatchHeightMeters;
            return point;
        }

        public static float FeetToMeters(float feet)
        {
            return feet / 3.2808f;
        }

        public static Color ThermalColorToColor(ThermalColor thermalColor)
        {
            return UintToColor((uint)thermalColor);
        }

        public static Color MapColorToColor(MapColor mapColor)
        {
            return UintToColor((uint)mapColor);
        }

        public static Color UintToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);

            return Color.FromArgb(a, r, g, b);
        }

        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        public static void WriteLine(string text, ref string outputText)
        {
            Write(text + Console.Out.NewLine, ref outputText);
        }

        // Either outputs to the console or appends to OutputText, depending on if using the CLI or GUI.
        public static void Write(string text, ref string outputText)
        {
            if (outputText == null)
            {
                Console.Write(text);
            }
            else
            {
                outputText += text;
            }
        }

        // Returns a string that can be used to output properly named tiles from QGIS's Atlas layout.
        public static string GetAtlasString(int width, int height)
        {
            string atlasString = string.Empty;

            for (int i = width - 1; i >= 0; i--)
            {
                for (int j = height - 1; j >= 0; j--)
                {
                    string tileName = string.Format("{0:00}{1:00}", i, j);
                    atlasString += string.Format(",'{0}'", tileName);
                }
            }

            // @atlas_featurenumber starts a 1, so we need to add a zeroth entry before adding the string we built above.
            return string.Format("array_get(array('0'{0}), @atlas_featurenumber)", atlasString);
        }
    }
}
