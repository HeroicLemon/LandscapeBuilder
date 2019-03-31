using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Drawing;

namespace LandscapeBuilderLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SettingsManager
    {
        #region Directories
        // The input directory where the reference maps are stored.
        [JsonProperty]
        public string InputDir { get; set; }
        public string InputAtlasDir { get { return Path.Combine(InputDir, "Atlas"); } }
        public string InputAirportDir { get { return Path.Combine(InputDir, "AirportData"); } }

        // The intermediary outputs.
        [JsonProperty]
        public string OutputDir { get; set; }
        public string OutputTextureDir { get { return Path.Combine(OutputDir, "Textures"); } }
        public string OutputTexturePatchDir { get { return Path.Combine(OutputTextureDir, "Patches"); } }
        public string OutputThermalMapDir { get { return Path.Combine(OutputDir, "ThermalMaps"); } }
        public string OutputThermalMapTilesDir {  get { return Path.Combine(OutputThermalMapDir, "Tiles"); } }
        public string OutputForestMapTilesDir { get { return Path.Combine(OutputDir, "ForestMaps"); } }
        public string OutputAirportsObjDir { get { return Path.Combine(OutputDir, "Airports"); } }

        // The final outputs to be used in Condor.
        public string OutputFinalDir { get; set; }
        public string OutputDDSDir { get { return Path.Combine(OutputFinalDir, "Textures"); } }
        public string OutputForestMapDir { get { return Path.Combine(OutputFinalDir, "ForestMaps"); } }
        public string OutputHeightMapDir { get { return Path.Combine(OutputFinalDir, "HeightMaps"); } }
        public string OutputAirportsDir {  get { return Path.Combine(OutputFinalDir, "Airports"); } }

        // Local app data for storing settings.
        public string AppDataDir { get; private set; }

        // Landscape directory of Condor 2
        public string CondorLandscapesDir { get; private set; }
        public string CurrentLandscapeDir { get { return Path.Combine(CondorLandscapesDir, LandscapeName); } }

        public string ExecutableDir { get; private set; }
        #endregion

        private string _landscapeName = string.Empty;
        [JsonProperty]
        public string LandscapeName
        {
            get { return _landscapeName; }
            set
            {
                _landscapeName = value;
                updateLandscape();
            }
        }

        public PointF LatLongTopLeft { get; private set; }
        public PointF LatLongBottomRight { get; private set; }

        public static readonly int HeightMapResolution = 30;
        public static readonly int PatchHeightMeters = 5760;

        private static readonly Lazy<SettingsManager> lazy = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance {  get { return lazy.Value; } }
        
        public void InitSettings()
        {
            // Save the default directories.
            initializeDefaultDirectories();
            findCondor();
            if (File.Exists(Path.Combine(AppDataDir, "settings.conf")))
            {
                string json = File.ReadAllText(Path.Combine(AppDataDir, "settings.conf"));
                load(JsonConvert.DeserializeObject<SettingsManager>(json));
            }
            else
            {
                SaveSettings();
            }

            CreateDirectories();
        }

        // TODO: Find a more maintainable way of doing this.
        private void load(SettingsManager other)
        {
            OutputDir = other.OutputDir;
            OutputFinalDir = Path.Combine(OutputDir, "Final");
            InputDir = other.InputDir;
            LandscapeName = other.LandscapeName;
        }

        private void initializeDefaultDirectories()
        {

            ExecutableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);

            // Inputs can be set from command line argumente.
            InputDir = Path.Combine(ExecutableDir, "Atlas");

            // This will be customizable.
            OutputDir = Path.Combine(ExecutableDir, "BuilderOutput");

            // Will either be this 'Final' subdirectory or the landscape directory.
            OutputFinalDir = Path.Combine(OutputDir, "Final");

            AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LandscapeBuilder");
        }

        public void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path.Combine(AppDataDir, "settings.conf"), json);
        }

        public void CreateDirectories()
        {
            // Create all the output directories if they don't already exist.
            Directory.CreateDirectory(OutputDir);
            Directory.CreateDirectory(OutputTextureDir);
            Directory.CreateDirectory(OutputTexturePatchDir);
            Directory.CreateDirectory(OutputForestMapTilesDir);
            Directory.CreateDirectory(OutputThermalMapDir);
            Directory.CreateDirectory(OutputThermalMapTilesDir);
            Directory.CreateDirectory(OutputAirportsObjDir);

            Directory.CreateDirectory(OutputFinalDir);
            Directory.CreateDirectory(OutputDDSDir);
            Directory.CreateDirectory(OutputForestMapDir);
            Directory.CreateDirectory(OutputHeightMapDir);
            Directory.CreateDirectory(OutputAirportsDir);

            Directory.CreateDirectory(AppDataDir);
        }

        private void findCondor()
        {
            string reg = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\";
            string[] subkeys = Registry.LocalMachine.OpenSubKey(reg).GetSubKeyNames();
            foreach (string item in subkeys)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(reg + item);

                if (key.GetValue("DisplayName") != null)
                {
                    string displayName = key.GetValue("DisplayName").ToString();

                    if (displayName.Equals("Condor 2"))
                    {
                        CondorLandscapesDir = Path.Combine(key.GetValue("InstallLocation").ToString(), "Landscapes");
                        // If the landscape name hasn't been set yet, pick one as the default.
                        if (LandscapeName == null)
                        {
                            string landscapePath = Directory.GetDirectories(CondorLandscapesDir).FirstOrDefault();
                            LandscapeName = new DirectoryInfo(landscapePath).Name;
                        }
                        break;
                    }
                }
            }
        }

        private void updateLandscape()
        {
            if (CondorLandscapesDir != null)
            {
                string trnFile = string.Format("{0}.trn", LandscapeName);
                byte[] trnHeader = File.ReadAllBytes(Path.Combine(CurrentLandscapeDir, trnFile));

                // Parse the header
                int x = BitConverter.ToInt32(trnHeader, 0x00);
                int y = BitConverter.ToInt32(trnHeader, 0x04);
                float xRes = BitConverter.ToSingle(trnHeader, 0x08);
                float yRes = BitConverter.ToSingle(trnHeader, 0x0C);
                float dunno = BitConverter.ToSingle(trnHeader, 0x10);
                float bottomRightEasting = BitConverter.ToSingle(trnHeader, 0x14);
                float bottomRightNorthing = BitConverter.ToSingle(trnHeader, 0x18);
                int utmZone = BitConverter.ToInt32(trnHeader, 0x1C);
                char utmHemisphere = BitConverter.ToChar(trnHeader, 0x20);

                float topLeftEasting = bottomRightEasting - (x * xRes);
                float topLeftNorthing = bottomRightNorthing - (y * yRes);

                LatLongTopLeft = Utilities.UTMToLatLong(topLeftNorthing, topLeftEasting, utmZone, utmHemisphere);
                LatLongBottomRight = Utilities.UTMToLatLong(bottomRightNorthing, bottomRightEasting, utmZone, utmHemisphere);
            }
        }
    }
}
