using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace LandscapeBuilderLib
{
    static class Utilities
    {
        public static double GetSlope(Point p1, Point p2)
        {
            return (double)(p2.Y - p1.Y) / (double)(p2.X - p1.X);
        }

        // Uses CoCoCo to translate the lat/long corners into the landscape's XY coordinates.
        public static PointF LatLongToLandscapeXY(PointF latLong, RunwayCorner corner, ref bool missingCoCoCo)
        {
            PointF landscapeXY = new Point(-1, -1);

            Process process = new Process();
            process.StartInfo.FileName = "CoCoCo.exe";
            process.StartInfo.Arguments = string.Format("{0} {1} {2}", SettingsManager.Instance.LandscapeName, latLong.Y, latLong.X);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            // Make sure working directory is set up properly so that CoTaCo.ini can be parsed.
            if (!File.Exists(Path.Combine(SettingsManager.Instance.Executable, process.StartInfo.FileName)))
            {
                foreach (string path in Environment.GetEnvironmentVariable("PATH").Split(';'))
                {
                    if (File.Exists(Path.Combine(path, process.StartInfo.FileName)))
                    {
                        process.StartInfo.WorkingDirectory = path;
                        break;
                    }
                }
            }

            try
            {
                process.Start();

                float posX = float.MinValue;
                float posY = float.MinValue;
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (line.Contains("TPPosX"))
                    {
                        float.TryParse(line.Substring(line.IndexOf('=') + 1), out posX);
                    }
                    else if (line.Contains("TPPosY"))
                    {
                        float.TryParse(line.Substring(line.IndexOf('=') + 1), out posY);
                    }
                }

                process.WaitForExit();


                if (posX > float.MinValue && posY > float.MinValue)
                {
                    // Round outwards to the nearest value divisible by the heightmap's resolution.
                    switch (corner)
                    {
                        case RunwayCorner.TopLeft:
                            {
                                landscapeXY.X = posX;
                                landscapeXY.Y = posY;
                            }
                            break;
                        case RunwayCorner.TopRight:
                            {
                                landscapeXY.X = posX;
                                landscapeXY.Y = posY;
                            }
                            break;
                        case RunwayCorner.BottomRight:
                            {
                                landscapeXY.X = posX;
                                landscapeXY.Y = posY;
                            }
                            break;
                        case RunwayCorner.BottomLeft:
                            {
                                landscapeXY.X = posX;
                                landscapeXY.Y = posY;
                            }
                            break;
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                missingCoCoCo = true;
                //writeLine(string.Format("Failed to run {0}. Ensure {0} is in '{1}' or set up in the Path environment variable", process.StartInfo.FileName, SettingsManager.Instance.Executable));
            }

            return landscapeXY;
        }
               
        public static Point GetTileCoordinatesFromName(string tileName)
        {
            int tileX = int.Parse(tileName.Substring(0, 2));
            int tileY = int.Parse(tileName.Substring(2, 2));

            return new Point(tileX, tileY);
        }
    }
}
