using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using Newtonsoft.Json;

namespace LandscapeBuilderLib
{
    #region Enums
    enum MapColor : uint
    {
        // National Land Coverage Data
        DevelopedOpenSpace = 0xffddc9c9,       // 21
        DevelopedLowIntensity = 0xffd89382,     // 22
        DevelopedMediumIntensity = 0xffed0000,  // 23
        DevelopedHighIntensity = 0xffaa0000,    // 24
        BarrenLand = 0xffb2ada3,                // 31
        ForestDeciduous = 0xff68aa63,           // 41
        ForestConiferous = 0xff1c6330,          // 42  
        ForestMixed = 0xffb5c98e,               // 43
        Grassland = 0xffe2e2c1,                 // 71
        Pasture = 0xffdbd83d,                   // 81
        CultivatedCrops = 0xffaa7028,           // 82
        WetlandsWoody = 0xffbad8ea,             // 90
        WetlandsEmergent = 0xff70a3ba,          // 95    

        // Other
        RoadPaved = 0xff3c4345,
        RoadGravel = 0xff565f62,
        RoadDirt = 0xff987d1e,
        Railway = 0xff3c4344,
        Aerodrome = 0xff33ff00,
        Runway = 0xff606060,
        RunwayGrass = 0xff0a7c00,
        RunwayDirt = 0xff606025,
        Water = 0xff00f7ff

    }

    public enum ThermalColor : uint
    {
        None = 0xff000000,
        Weak = 0xff404040,
        Moderate = 0xff666666,
        Best = 0xffB2B2B2,
        //Custom = 0x00000000
    }

    // For the .for file:
    //    0 is no trees.
    //    1 is deciduous trees.
    //    2 is coniferous trees.
    //    3 is mixed.
    public enum ForestType : byte
    {
        None = 0x00,
        Coniferous = 0x01,
        Deciduous = 0x02,
        Mixed = 0x03
    }

    enum RunwayCorner : int
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }
    #endregion

    public class LandscapeBuilder
    {
        private string _landscapeName;
        private int _landscapeWidth, _landscapeHeight; // Width and height of the map in tiles.
        private string _singleTile;

        public Dictionary<Color, LandData> Textures { get; set; } = new Dictionary<Color, LandData>();
        public List<Airport> Airports { get; set; }
        private Color _defaultColor;
        private const int heightmapResolution = 30;

        public string OutputDirectory {  get { return _directoryManager.Output; } set { _directoryManager.Output = value; } }
        public string InputDirectory { get { return _directoryManager.InputMap; } set { _directoryManager.InputMap = value; } }
        public string CondorDirectory {  get { return _directoryManager.CondorLandscape; } }

        private bool _outputToConsole = true;
        public string OutputText { get; set; }

        DirectoryManager _directoryManager;

        public LandscapeBuilder(bool outputToConsole = true)
        {
            _outputToConsole = outputToConsole;
            InitializeDirectories();
            InitializeAirports();

            _landscapeName = "CentralVA";
            generateAirports();
        }

        // landscapeName is the name of the landscape in Condor. It is used for naming some files (e.g., [LandscapeName].tdm) and, if outputToCondor is true, determines where to write the final files.
        // genDDS determines if nvdxt.exe is used to build the DDS files actually used as textures by Condor.
        // genForestFiles determines if the .for forest files are generated.
        // genThermalFile termines if the .tdm thermal file is generated.
        // If outputToCondor is true, the output files will be written to the landscape's directory (determined by landscapeName). If it is false, the files will be written to a 'Final' directory in the Output directory. 
        // outputDir is the path to where the intermediary files should be written, and the Final outputs if outputToCondor is false.
        // atlasDir is the directory of the input tiles.
        // textureDir is the directory of the texture files.
        // singleTile specifies a single tile that can be generated for testing purposes.
        public void Build(string landscapeName = "", bool genDDS = false, bool genForestFiles = false, bool genThermalFile = false, bool outputToCondor = false, string outputDir = "", string atlasDir = "", string singleTile = "")
        {
            // If the landscape name is not provided, offer selection from the existing landscapes.
            if(landscapeName == string.Empty)
            {
                if(_directoryManager.CondorLandscape == string.Empty)
                {
                    writeLine("Condor not found! Any output files that use the landscape name will be given generic names.");
                    outputToCondor = false;
                }
                else
                {
                    string[] landscapes = Directory.GetDirectories(_directoryManager.CondorLandscape);
                    int i = 1;
                    writeLine("Use number keys to select a landscape.");
                    writeLine("Hit any other key to continue without selecting. Any output files that use the landscape name will be given generic names.");
                    foreach(string landscape in landscapes)
                    {
                        writeLine(string.Format("{0}. {1}", i, landscape.Substring(_directoryManager.CondorLandscape.Length + 1)));
                        i++;
                    }

                    string input = Console.ReadLine();
                    int landscapeIndex = -1;
                    if(int.TryParse(input, out landscapeIndex))
                    {
                        landscapeIndex--; // displayed entries are 1 based.
                        if(landscapeIndex < landscapes.Length)
                        {
                            _landscapeName = landscapes[landscapeIndex].Substring(_directoryManager.CondorLandscape.Length + 1);

                            if (outputToCondor)
                            {
                                _directoryManager.OutputFinal = landscapes[landscapeIndex];
                            }
                        }
                    }
                    else
                    {
                        outputToCondor = false;
                    }
                }
            }
            else
            {
                _landscapeName = landscapeName;

                if(outputToCondor)
                { 
                    string path = Path.Combine(_directoryManager.CondorLandscape, landscapeName);
                    // Make sure this landscape exists
                    if (Directory.Exists(path))
                    {
                        _directoryManager.OutputFinal = path;
                    }
                    else
                    {
                        writeLine(string.Format("Landscape {0} not found. Final outputs will be in {1}", landscapeName, _directoryManager.OutputFinal));
                        outputToCondor = false;
                    }
                }
            }

            if(outputDir != string.Empty)
            {
                _directoryManager.Output = outputDir;
                Directory.CreateDirectory(_directoryManager.Output);

                if(!outputToCondor)
                {
                    _directoryManager.OutputFinal = Path.Combine(_directoryManager.Output, "Final");
                }
            }

            if(atlasDir != string.Empty)
            {
                if (Directory.Exists(atlasDir))
                {
                    _directoryManager.InputMap = atlasDir;
                }
                else
                {
                    writeLine(string.Format("{0} not found, using default of {1}", atlasDir, _directoryManager.InputMap));
                }
            }

            _singleTile = singleTile;

            _directoryManager.CreateDirectories();
            InitializeTextures();
            getLandscapeWidthAndHeight();

            OutputText = string.Empty;
            writeLine("Beginning Processing...");
            Stopwatch elapsed = Stopwatch.StartNew();

            // Create the intermediary outputs.
            build();

            // Generate DDS textures
            if (genDDS)
            {
                generateDDS();
            }

            // Generate the .for forest files.
            if (genForestFiles)
            {
                generateForestFiles();

                if (outputToCondor)
                {
                    // If we're writing to the Condor directory, we need to generate the hashes.
                    generateHashes();
                }
            }

            // Generate the .tdm thermal file.
            if (genThermalFile)
            {
                generateThermalFile();
            }

            elapsed.Stop();
            writeLine(string.Format("Finished processing! Total elapsed time: {0} hours, {1} minutes, {2} seconds.", elapsed.Elapsed.Hours, elapsed.Elapsed.Minutes, elapsed.Elapsed.Seconds));
        }

        private void getLandscapeWidthAndHeight()
        {
            // Determine the height and width of the map in tiles from the atlas input.
            string[] mapFiles = Directory.GetFiles(_directoryManager.InputMap, "*.png", SearchOption.TopDirectoryOnly);
            string lastTile = Path.GetFileNameWithoutExtension(mapFiles.Last());
            Point tileCoordinates = getTileCoordinatesFromName(lastTile);
            _landscapeWidth = tileCoordinates.X + 1;
            _landscapeHeight = tileCoordinates.Y + 1;
        }

        // Generates the intermediate tile textures, forest maps, and thermal map.
        private void build()
        {
            string[] mapFiles = Directory.GetFiles(_directoryManager.InputMap, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string file in mapFiles)
            {
                if (_singleTile == string.Empty || _singleTile == Path.GetFileNameWithoutExtension(file))
                {
                    buildTile(file);
                }
            }

            // Generate the thermal map from the tile outputs, it's one big image.
            generateThermalMap();
        }

        private void buildTile(string strFile)
        { 
            string tileName = Path.GetFileNameWithoutExtension(strFile);
            write(string.Format("Processing tile {0}...", tileName));
            Stopwatch elapsed = Stopwatch.StartNew();

            // Load the input map.
            BitmapWrapper mapInput = new BitmapWrapper(strFile);

            // Create bitmaps for the outputs.
            BitmapWrapper textureOutput = new BitmapWrapper(8192, 8192, PixelFormat.Format32bppArgb, ImageLockMode.WriteOnly);
            BitmapWrapper deciduousOutput = new BitmapWrapper(8192, 8192, PixelFormat.Format32bppArgb, ImageLockMode.WriteOnly);
            BitmapWrapper coniferousOutput = new BitmapWrapper(8192, 8192, PixelFormat.Format32bppArgb, ImageLockMode.WriteOnly);
            BitmapWrapper thermalOutput = new BitmapWrapper(8192, 8192, PixelFormat.Format32bppArgb, ImageLockMode.WriteOnly);

            for (int i = 0; i < mapInput.Width; i++)
            {
                for(int j = 0; j < mapInput.Height; j++)
                {
                    // Get the color of the current pixel in the map.
                    Color mapColor = mapInput.GetPixel(i, j);

                    LandData landData;
                    // Get the data 
                    if (Textures.ContainsKey(mapColor))
                    {
                        landData = Textures[mapColor];
                    }
                    else
                    {
                        landData = Textures[_defaultColor];
                    }                     

                    // Set pixel colors for output bitmaps.
                    textureOutput.SetPixel(i, j, landData.GetColor(i, j));
                    deciduousOutput.SetPixel(i, j, landData.DeciduousForestColor);
                    coniferousOutput.SetPixel(i, j, landData.ConiferousForestColor);
                    thermalOutput.SetPixel(i, j, landData.ThermalColor);
                }
            }

            // Save the bitmaps
            textureOutput.Save(string.Format(Path.Combine(_directoryManager.OutputTexture, @"{0}.bmp"), tileName));
            saveTexturePatches(textureOutput.Bitmap, tileName);
            
            deciduousOutput.Save(string.Format(Path.Combine(_directoryManager.OutputForestMapTiles, @"b{0}.bmp"), tileName), 2048);
            coniferousOutput.Save(string.Format(Path.Combine(_directoryManager.OutputForestMapTiles, @"s{0}.bmp"), tileName), 2048);
            thermalOutput.Save(string.Format(Path.Combine(_directoryManager.OutputThermalMapTiles, @"{0}.bmp"), tileName), 256);

            textureOutput.Dispose();
            deciduousOutput.Dispose();
            coniferousOutput.Dispose();
            thermalOutput.Dispose();

            elapsed.Stop();
            writeLine(string.Format("Done! Elapsed time: {0} minutes, {1} seconds.", elapsed.Elapsed.Minutes, elapsed.Elapsed.Seconds));
        }

        // Splits a tile into its 16 patches.
        private Dictionary<string, Bitmap> splitTileToPatches(Bitmap bitmap, string tileName)
        {
            Dictionary<string, Bitmap> patches = new Dictionary<string, Bitmap>();
            Point tileCoordinates = getTileCoordinatesFromName(tileName);
            int tileX = tileCoordinates.X;
            int tileY = tileCoordinates.Y;

            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    int patchX = tileX * 4 + i;
                    int patchY = tileY * 4 + j;
                    string patchName = string.Format("{0:00}{1:00}", patchX, patchY);

                    // Start at the bottom right.
                    int patchAreaX = (3 - i) * bitmap.Width / 4;
                    int patchAreaY = (3 - j) * bitmap.Height / 4;
                    Rectangle patchArea = new Rectangle(patchAreaX, patchAreaY, bitmap.Width / 4, bitmap.Width / 4);
                    Bitmap patch = bitmap.Clone(patchArea, bitmap.PixelFormat);
                    patches.Add(patchName, patch);
                }
            }

            return patches;
        }

        private void saveTexturePatches(Bitmap bitmap, string tileName)
        {
            Dictionary<string, Bitmap> patches = splitTileToPatches(bitmap, tileName);

            foreach(var patch in patches)
            {
                string patchName = string.Format("t{0}.bmp", patch.Key);
                patch.Value.Save(Path.Combine(_directoryManager.OutputTexturePatch, patchName), ImageFormat.Bmp);
            }
        }

        // Generate the .for files and create the forest hash.
        // Technically, it would be faster to skip writing out the forest map bitmaps and generate these directly instead, but it only adds a few minutes to the total processing time.
        private void generateForestFiles()
        {
            string[] mapFiles = Directory.GetFiles(_directoryManager.OutputForestMapTiles, "b*.bmp", SearchOption.TopDirectoryOnly);
            foreach(string file in mapFiles)
            {
                string tileName = Path.GetFileNameWithoutExtension(file).Substring(1);
                write(string.Format("Generating forest file for tile {0}...", tileName));
                Stopwatch elapsed = Stopwatch.StartNew();

                Bitmap deciduousTile = new Bitmap(file);
                Dictionary<string, Bitmap> deciduousPatches = splitTileToPatches(deciduousTile, tileName);
                deciduousTile.Dispose();

                string coniferousPath = string.Format(Path.Combine(Path.GetDirectoryName(file), "s{0}.bmp"), tileName);
                Bitmap coniferousTile = new Bitmap(coniferousPath);
                Dictionary<string, Bitmap> coniferousPatches = splitTileToPatches(coniferousTile, tileName);
                coniferousTile.Dispose();

                for (int k = 0; k < deciduousPatches.Count; k++)
                {
                    BitmapWrapper deciduousPatch = new BitmapWrapper(deciduousPatches.Values.ElementAt(k));
                    BitmapWrapper coniferousPatch = new BitmapWrapper(coniferousPatches.Values.ElementAt(k));
                    byte[] forestData = new byte[deciduousPatch.Width * deciduousPatch.Height];

                    for (int i = 0; i < deciduousPatch.Width; i++)
                    {
                        for (int j = 0; j < deciduousPatch.Height; j++)
                        {
                            // Start at the bottom right.
                            int bitmapI = deciduousPatch.Width - i - 1;
                            int bitmapJ = deciduousPatch.Height - j - 1;
                            Color deciduousColor = deciduousPatch.GetPixel(bitmapI, bitmapJ);
                            Color coniferousColor = coniferousPatch.GetPixel(bitmapI, bitmapJ);

                            // Black color means no trees present, anything else means trees present.
                            byte currentData = (byte)ForestType.None;
                            if (deciduousColor.ToArgb() != Color.Black.ToArgb() && coniferousColor.ToArgb() != Color.Black.ToArgb())
                            {
                                currentData = (byte)ForestType.Mixed;
                            }
                            else if (deciduousColor.ToArgb() != Color.Black.ToArgb())
                            {
                                currentData = (byte)ForestType.Deciduous;
                            }
                            else if (coniferousColor.ToArgb() != Color.Black.ToArgb())
                            {
                                currentData = (byte)ForestType.Coniferous;
                            }

                            forestData[(i * deciduousPatch.Width) + j] = currentData;
                        }
                    }

                    string path = string.Format(Path.Combine(_directoryManager.OutputForestMap, "{0}.for"), deciduousPatches.Keys.ElementAt(k));
                    File.WriteAllBytes(path, forestData);
                }
                elapsed.Stop();
                writeLine(string.Format("Done! Elapsed time: {0} minutes, {1} seconds.", elapsed.Elapsed.Minutes, elapsed.Elapsed.Seconds));
            }
        }

        // Generates the terrain and forest hashes. This program does not alter the terrain, but it does alter the forests so this needs to be done.
        private void generateHashes()
        {
            if (_landscapeName != string.Empty)
            {
                write("Generating hashes...");

                Process process = new Process();
                process.StartInfo.FileName = "LandscapeEditor.exe";
                process.StartInfo.Arguments = string.Format("-hash {0}", _landscapeName);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = _outputToConsole;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += new DataReceivedEventHandler(processOutput);           

                try
                {
                    process.Start();
                    if (_outputToConsole)
                    {
                        process.BeginOutputReadLine();
                    }
                    process.WaitForExit();
                    writeLine("Done!");
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    writeLine(string.Format("Failed to generate hashes. Ensure {0} is in '{1}' or set up in the Path environment variable", process.StartInfo.FileName, _directoryManager.Executable));
                }
            }
        }

        // Generates the actual DDS texture files.
        private void generateDDS()
        {
            writeLine("Generating DDS...");
            Stopwatch elapsed = Stopwatch.StartNew();

            Process process = new Process();
            process.StartInfo.FileName = "nvdxt.exe";
            process.StartInfo.Arguments = string.Format("-quality_highest -nmips 12 -dxt3 -Triangle -file \"{0}\" -outdir \"{1}\"", Path.Combine(_directoryManager.OutputTexturePatch, "t*.bmp"), _directoryManager.OutputDDS);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = _outputToConsole;
            process.OutputDataReceived += new DataReceivedEventHandler(processOutput);

            try
            {
                process.Start();
                if (_outputToConsole)
                {
                    process.BeginOutputReadLine();
                }
                process.WaitForExit();

                elapsed.Stop();
                writeLine(string.Format("Done! Elapsed time: {0} hours, {1} minutes, {2} seconds.", elapsed.Elapsed.Hours, elapsed.Elapsed.Minutes, elapsed.Elapsed.Seconds));
            }
            catch (System.ComponentModel.Win32Exception)
            {
                elapsed.Stop();
                writeLine(string.Format("Failed to generate DDS. Ensure {0} is in '{1}' or set up in the Path environment variable", process.StartInfo.FileName, _directoryManager.Executable));
            }
        }

        // Creates a single thermal map from the per tile thermal maps.
        // Again, it may be faster to do this during generation instead of spitting out the intermediary bitmaps, but this only adds a little bit of time.
        private void generateThermalMap()
        {
            write("Building thermal map...");
            Stopwatch elapsed = Stopwatch.StartNew();

            string[] mapFiles = Directory.GetFiles(_directoryManager.OutputThermalMapTiles, "*.bmp", SearchOption.TopDirectoryOnly);
            BitmapWrapper thermalMap = new BitmapWrapper(_landscapeWidth * 256, _landscapeHeight * 256, PixelFormat.Format32bppArgb, ImageLockMode.WriteOnly);
            foreach (string file in mapFiles)
            {
                string currentTileName = Path.GetFileNameWithoutExtension(file);
                Point currentTileCoordinates = getTileCoordinatesFromName(currentTileName);
                BitmapWrapper currentTile = new BitmapWrapper(file);
                thermalMap.CopyToTile(currentTileCoordinates.X, currentTileCoordinates.Y, currentTile);
            }

            // Go ahead and save this in case manual import of thermal map is desired later.
            thermalMap.Save(Path.Combine(_directoryManager.OutputThermalMap, "ThermalMap.bmp"));

            elapsed.Stop();
            writeLine(string.Format("Done! Elapsed time: {0} minutes, {1} seconds.", elapsed.Elapsed.Minutes, elapsed.Elapsed.Seconds));
        }

        // Generates the .tdm thermal file from the map.
        private void generateThermalFile()
        {
            write("Generating thermal file...");
            Stopwatch elapsed = Stopwatch.StartNew();

            BitmapWrapper thermalMap = new BitmapWrapper(Path.Combine(_directoryManager.OutputThermalMap, "ThermalMap.bmp"));

            // The TDM file starts at the bottom right and goes all the way across the width of the map (no tiles).
            // Each pixel in ThermalMap.bmp is represented by a single byte of one of the channels of the color.
            byte[] tdmData = new byte[thermalMap.Width * thermalMap.Height + 8];
            // Write landscape width and height to header.
            tdmData[1] = (byte)_landscapeWidth;
            tdmData[5] = (byte)_landscapeHeight;

            for (int j = 0; j < thermalMap.Height; j++)
            {
                for (int i = 0; i < thermalMap.Width; i++)
                {
                    int bitmapI = thermalMap.Width - i - 1;
                    int bitmapJ = thermalMap.Height - j - 1;
                    Color color = thermalMap.GetPixel(bitmapI, bitmapJ);
                    int index = (j * thermalMap.Width) + i + 8;
                    tdmData[index] = color.R;
                }
            }

            string fileName = _landscapeName == string.Empty ? "[LandscapeName]" : _landscapeName;
            string path = string.Format(Path.Combine(_directoryManager.OutputFinal, "{0}.tdm"), fileName);
            File.WriteAllBytes(path, tdmData);

            elapsed.Stop();
            writeLine(string.Format("Done! Elapsed time: {0} minutes, {1} seconds.", elapsed.Elapsed.Minutes, elapsed.Elapsed.Seconds));
        }

        // Generates the .apt airport file and flattens the terrain in the .tr3 files.
        private void generateAirports()
        {
            byte[] bytes = null;
            foreach (Airport airport in Airports)
            {
                // Combine all of the bytes to use for the .apt file
                if (bytes == null)
                {
                    bytes = airport.GetBytes();
                }
                else
                {
                    bytes = bytes.Concat(airport.GetBytes()).ToArray();
                }

                // If we have data for the corners, also flatten the terrain.
                // TODO: Maybe spit out the airports that were missing data to alert the user as to which ones need to be done manually?
                if(airport.RunwayCorners != null && airport.Name == "Hanover Co Muni")
                {
                    flattenTerrain(airport.RunwayCorners, airport.Altitude);
                }
            }

            // Write to .apt file.
            File.WriteAllBytes(@"D:\Program Files (x86)\Condor2\Landscapes\CentralVA\CentralVA.apt", bytes);
        }

        // Flattens the area containing the rectangle specified in corners to the given elevation.
        // Updates the .tr3 heightmap files.
        private void flattenTerrain(PointF[] cornersLatLong, float elevation)
        {            
            Point[] cornersLandscapeXY = new Point[cornersLatLong.Length];
            bool missingCoCoCo = false;
            for(int i = 0; i < cornersLatLong.Length; i++)
            {
                // TODO: This is assuming that 0 is TL, 1 is TR, 2 is BR and 3 is BL. Need to confirm this is true for all cases.
                // Convert lat/long to the landscape's XY coordinates.
                cornersLandscapeXY[i] = latLongToLandscapeXY(cornersLatLong[i], (RunwayCorner)i, ref missingCoCoCo);

                if(missingCoCoCo)
                {
                    break;
                }
            }

            // Check to see if we managed to get the new points.
            if(!cornersLandscapeXY.Contains(new Point(-1, -1)))
            {
                Point topLeft = cornersLandscapeXY[(int)RunwayCorner.TopLeft];
                Point bottomLeft = cornersLandscapeXY[(int)RunwayCorner.BottomLeft];
                Point bottomRight = cornersLandscapeXY[(int)RunwayCorner.BottomRight];
                Point topRight = cornersLandscapeXY[(int)RunwayCorner.TopRight];

                // This is the list of point that make up the outline of the region that needs to be flattened.
                List<PointF> boundingPoints = new List<PointF>();
                List<byte> boundingTypes = new List<byte>();

                // Calculate the slopes
                double leftSlope = getSlope(topLeft, bottomLeft);
                double bottomSlope = getSlope(bottomLeft, bottomRight);
                double rightSlope = getSlope(bottomRight, topRight);
                double topSlope = getSlope(topRight, topLeft);

                int x2, y2;
                // First add the top left point.
                boundingPoints.Add(topLeft);      
                
                // Then add the points along the left side of the runway.
                y2 = topLeft.Y;
                while(y2 > bottomLeft.Y)
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
                while(x2 > bottomRight.X)
                {
                    x2 -= heightmapResolution;
                    y2 = roundDownToHeightMapRes((x2 - bottomLeft.X) * bottomSlope + bottomLeft.Y);
                    boundingPoints.Add(new PointF(x2, y2));
                }

                // Then add the bottom right point.
                boundingPoints.Add(bottomRight);

                // Then add the points along the right side of the runway.
                y2 = bottomRight.Y;
                while(y2 < topRight.Y)
                {
                    y2 += heightmapResolution;
                    x2 = roundDownToHeightMapRes(((y2 - bottomRight.Y) / rightSlope) + bottomRight.X);
                    boundingPoints.Add(new PointF(x2, y2));
                }

                // Then add the top right point
                boundingPoints.Add(topRight);

                // Then add the points along the top side of the runway.
                x2 = topRight.X;
                while(x2 < topLeft.X)
                {
                    x2 += heightmapResolution;
                    y2 = roundUpToHeightMapRes((x2 - topRight.X) * topSlope + topRight.Y);
                    boundingPoints.Add(new PointF(x2, y2));
                }

                // TODO: This gets me the boundary...is there a way to fill it in while determining it?
                string testing = string.Empty;
                foreach(PointF point in boundingPoints)
                {
                    testing += string.Format("-{0},{1}\n", point.X, point.Y);
                }

                int i = 0;
            }
        }

        private double getSlope(Point p1, Point p2)
        {
            return (double)(p2.Y - p1.Y) / (double)(p2.X - p1.X);
        }

        // Uses CoCoCo to translate the lat/long corners into the landscape's XY coordinates.
        private Point latLongToLandscapeXY(PointF latLong, RunwayCorner corner, ref bool missingCoCoCo)
        {
            Point landscapeXY = new Point(-1, -1);

            Process process = new Process();
            process.StartInfo.FileName = "CoCoCo.exe";
            process.StartInfo.Arguments = string.Format("{0} {1} {2}", _landscapeName, latLong.Y, latLong.X);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            // Make sure working directory is set up properly so that CoTaCo.ini can be parsed.
            if(!File.Exists(Path.Combine(_directoryManager.Executable, process.StartInfo.FileName)))
            {
                foreach(string path in Environment.GetEnvironmentVariable("PATH").Split(';'))
                {
                    if(File.Exists(Path.Combine(path, process.StartInfo.FileName)))
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
                                landscapeXY.X = roundUpToHeightMapRes(posX);
                                landscapeXY.Y = roundUpToHeightMapRes(posY);
                            }
                            break;
                        case RunwayCorner.TopRight:
                            {
                                landscapeXY.X = roundDownToHeightMapRes(posX);
                                landscapeXY.Y = roundUpToHeightMapRes(posY);
                            }
                            break;
                        case RunwayCorner.BottomRight:
                            {
                                landscapeXY.X = roundDownToHeightMapRes(posX);
                                landscapeXY.Y = roundDownToHeightMapRes(posY);
                            }
                            break;
                        case RunwayCorner.BottomLeft:
                            {
                                landscapeXY.X = roundUpToHeightMapRes(posX);
                                landscapeXY.Y = roundDownToHeightMapRes(posY);
                            }
                            break; 
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                missingCoCoCo = true;
                writeLine(string.Format("Failed to run {0}. Ensure {0} is in '{1}' or set up in the Path environment variable", process.StartInfo.FileName, _directoryManager.Executable));
            }

            return landscapeXY;
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

        private Point getTileCoordinatesFromName(string tileName)
        {
            int tileX = int.Parse(tileName.Substring(0, 2));
            int tileY = int.Parse(tileName.Substring(2, 2));

            return new Point(tileX, tileY);
        }

        // Handler to output from processes to the Console.
        private static void processOutput(object sendingProces, DataReceivedEventArgs outline)
        {
            if(!String.IsNullOrEmpty(outline.Data))
            {
                Console.WriteLine(outline.Data);
            }
        }

        public void InitializeDirectories()
        {
            _directoryManager = new DirectoryManager();
            if (File.Exists(Path.Combine(_directoryManager.AppData, "directories.conf")))
            {
                string json = File.ReadAllText(Path.Combine(_directoryManager.AppData, "directories.conf"));
                _directoryManager = JsonConvert.DeserializeObject<DirectoryManager>(json);
            }
            else
            {
                // Save the default directories.
                SaveDirectories();
            }
        }

        public void InitializeAirports()
        {
            if (File.Exists(Path.Combine(_directoryManager.AppData, "airports.conf")))
            {
                string json = File.ReadAllText(Path.Combine(_directoryManager.AppData, "airports.conf"));
                Airports = JsonConvert.DeserializeObject<List<Airport>>(json);
            }
            else
            {
                FAAShapefileRunwayParser parser = new FAAShapefileRunwayParser();
                Airports = parser.Parse();
                //SaveAirports();
            }
        }

        // Loads the textures defined in textures.conf or initializes the defaults if textures.conf does not exist.
        public void InitializeTextures()
        {
            if (File.Exists(Path.Combine(_directoryManager.AppData, "textures.conf")))
            {
                string json = File.ReadAllText(Path.Combine(_directoryManager.AppData, "textures.conf"));
                Textures = JsonConvert.DeserializeObject<Dictionary<Color, LandData>>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }
            else
            {
                initializeDefaultTextures();
            }

            // Determine the default color
            foreach(var texture in Textures)
            {
                if(texture.Value.IsDefault)
                {
                    _defaultColor = texture.Key;
                }
            }
        }
               
        private void initializeDefaultTextures()
        {
            // NLCD values
            Textures.Add(mapColorToColor(MapColor.Grassland), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-grass-green.jpg"), thermalColorToColor(ThermalColor.Moderate), "NLCD Grassland", ForestType.None, false, true));
            Textures.Add(mapColorToColor(MapColor.DevelopedHighIntensity), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-heavy-urban.jpg"), thermalColorToColor(ThermalColor.Best), "NLCD Developed, High Intensity"));
            Textures.Add(mapColorToColor(MapColor.DevelopedMediumIntensity), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-medium-urban-grass-green.jpg"), thermalColorToColor(ThermalColor.Best), "NLCD Developed, Medium Intensity"));
            Textures.Add(mapColorToColor(MapColor.DevelopedLowIntensity), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-Euro-city-farm1-mixed-forest.jpg"), thermalColorToColor(ThermalColor.Weak), "NLCD Developed, Low Itensity"));
            Textures.Add(mapColorToColor(MapColor.DevelopedOpenSpace), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-Euro-city-farm1-mixed-forest.jpg"), thermalColorToColor(ThermalColor.Weak), "NLCD Developed, Open Space"));
            Textures.Add(mapColorToColor(MapColor.CultivatedCrops), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-european-farm-1.jpg"), thermalColorToColor(ThermalColor.Best), "NLCD Cultivated Crops"));
            Textures.Add(mapColorToColor(MapColor.Pasture), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-european-farm-1.jpg"), thermalColorToColor(ThermalColor.Best), "NLCD Pasture/Hay"));
            Textures.Add(mapColorToColor(MapColor.ForestConiferous), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-forest-mixed-mixed-wilderness-3.jpg"), thermalColorToColor(ThermalColor.Weak), "NLCD Evergreen Forest", ForestType.Coniferous));
            Textures.Add(mapColorToColor(MapColor.ForestDeciduous), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-forest-mixed-mixed-wilderness-3.jpg"), thermalColorToColor(ThermalColor.Weak), "NLCD Deciduous Forest", ForestType.Deciduous));
            Textures.Add(mapColorToColor(MapColor.ForestMixed), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-forest-mixed-mixed-wilderness-3.jpg"), thermalColorToColor(ThermalColor.Weak), "NLCD Mixed Forest", ForestType.Mixed));
            Textures.Add(mapColorToColor(MapColor.WetlandsEmergent), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-swamp.jpg"), thermalColorToColor(ThermalColor.None), "NLCD Emergent Wetlands"));
            Textures.Add(mapColorToColor(MapColor.WetlandsWoody), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-swamp.jpg"), thermalColorToColor(ThermalColor.None), "NLCD Woody Wetlands"));
            Textures.Add(mapColorToColor(MapColor.BarrenLand), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-Western-barren2.jpg"), thermalColorToColor(ThermalColor.Moderate), "NLCD Barren Land (Rock/Sand/Clay)"));

            // Other
            Textures.Add(mapColorToColor(MapColor.Aerodrome), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-grass-green.jpg"), thermalColorToColor(ThermalColor.Moderate), "Aerodrome"));
            Textures.Add(mapColorToColor(MapColor.Runway), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-road-top.jpg"), thermalColorToColor(ThermalColor.Best), "Hard surface runways"));
            Textures.Add(mapColorToColor(MapColor.RunwayGrass), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-grass-yellow.jpg"), thermalColorToColor(ThermalColor.Moderate), "Grass runways"));
            Textures.Add(mapColorToColor(MapColor.RunwayDirt), new TexturedLandData(Path.GetFullPath(@"Textures/HITW-TS2-Western-barren-canyon-dirt.jpg"), thermalColorToColor(ThermalColor.Moderate), "Dirt runways"));
            Textures.Add(mapColorToColor(MapColor.RoadPaved), new ColoredLandData(mapColorToColor(MapColor.RoadPaved), thermalColorToColor(ThermalColor.Best), "Paved roads"));
            Textures.Add(mapColorToColor(MapColor.RoadGravel), new ColoredLandData(mapColorToColor(MapColor.RoadGravel), thermalColorToColor(ThermalColor.Weak), "Gravel roads"));
            Textures.Add(mapColorToColor(MapColor.RoadDirt), new ColoredLandData(mapColorToColor(MapColor.RoadDirt), thermalColorToColor(ThermalColor.Weak), "Dirt roads"));
            Textures.Add(mapColorToColor(MapColor.Water), new ColoredLandData(Color.FromArgb(0xff, 0x2a, 0x47, 0x4d), thermalColorToColor(ThermalColor.None), "Water", ForestType.None, true));
            SaveTextures();
        }

        public void SaveTextures()
        {
            string json = JsonConvert.SerializeObject(Textures, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            File.WriteAllText(Path.Combine(_directoryManager.AppData, "textures.conf"), json);
        }

        public void SaveDirectories()
        {
            string json = JsonConvert.SerializeObject(_directoryManager, Formatting.Indented);
            File.WriteAllText(Path.Combine(_directoryManager.AppData, "directories.conf"), json);
        }

        public void SaveAirports()
        {
            string json = JsonConvert.SerializeObject(Airports, Formatting.Indented);
            File.WriteAllText(Path.Combine(_directoryManager.AppData, "airports.conf"), json);
        }

        private Color thermalColorToColor(ThermalColor thermalColor)
        {
            return uintToColor((uint)thermalColor);
        }

        private Color mapColorToColor(MapColor mapColor)
        {
            return uintToColor((uint)mapColor);
        }

        private Color uintToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);

            return Color.FromArgb(a, r, g, b);
        }

        private Color changeColorBrightness(Color color, float correctionFactor)
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

        private void writeLine(string text)
        {
            write(text + Console.Out.NewLine);
        }

        // Either outputs to the console or appends to OutputText, depending on if using the CLI or GUI.
        private void write(string text)
        {
            if(_outputToConsole)
            {
                Console.Write(text);
            }
            else
            {
                OutputText += text;
            }
        }

        // Returns a string that can be used to output properly named tiles from QGIS's Atlas layout.
        // Doesn't really belong here, but it's a small function so whatever.
        public string GetAtlasString(int width, int height)
        {
            string atlasString = string.Empty;

            for(int i = width - 1; i >= 0; i--)
            {
                for(int j = height - 1; j >= 0; j--)
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
