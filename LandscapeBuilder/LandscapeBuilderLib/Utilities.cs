using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace LandscapeBuilderLib
{
    public static class Utilities
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
