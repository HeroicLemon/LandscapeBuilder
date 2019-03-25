using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace LandscapeBuilderLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SettingsManager
    {
        #region Directories
        // The input directory where the reference maps are stored.
        [JsonProperty]
        public string Input { get; set; }
        public string InputAtlas { get { return Path.Combine(Input, "Atlas"); } }
        public string InputAirport { get { return Path.Combine(Input, "AirportData"); } }

        // The intermediary outputs.
        [JsonProperty]
        public string Output { get; set; }
        public string OutputTexture { get { return Path.Combine(Output, "Textures"); } }
        public string OutputTexturePatch { get { return Path.Combine(OutputTexture, "Patches"); } }
        public string OutputThermalMap { get { return Path.Combine(Output, "ThermalMaps"); } }
        public string OutputThermalMapTiles {  get { return Path.Combine(OutputThermalMap, "Tiles"); } }
        public string OutputForestMapTiles { get { return Path.Combine(Output, "ForestMaps"); } }

        // The final outputs to be used in Condor.
        public string OutputFinal { get; set; }
        public string OutputDDS { get { return Path.Combine(OutputFinal, "Textures"); } }
        public string OutputForestMap { get { return Path.Combine(OutputFinal, "ForestMaps"); } }
        public string OutputHeightMap { get { return Path.Combine(OutputFinal, "HeightMaps"); } }

        // Local app data for storing settings.
        public string AppData { get; private set; }

        // Directory that Condor is installed to
        public string CondorLandscape { get; private set; }

        public string Executable { get; private set; }
        #endregion

        [JsonProperty]
        public string LandscapeName { get; set; }

        // TODO: Should be able to parse these from the condor files. .trn?
        public float LatitudeMax { get; set; } = 37.75f;
        public float LatitudeMin { get; set; } = 36.35f;
        public float LongitudeMax { get; set; } = -75.6f;
        public float LongitudeMin { get; set; } = -78.5f;

        public static readonly int HeightMapResolution = 30;
        public static readonly int PatchHeightMeters = 5760;

        private static readonly Lazy<SettingsManager> lazy = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance {  get { return lazy.Value; } }
        
        public void InitSettings()
        {
            // Save the default directories.
            initializeDefaultDirectories();
            findCondor();
            if (File.Exists(Path.Combine(AppData, "settings.conf")))
            {
                string json = File.ReadAllText(Path.Combine(AppData, "settings.conf"));
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
            this.Output = other.Output;
            this.Input = other.Input;
        }

        private void initializeDefaultDirectories()
        {

            Executable = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);

            // Inputs can be set from command line argumente.
            Input = Path.Combine(Executable, "Atlas");

            // This will be customizable.
            Output = Path.Combine(Executable, "BuilderOutput");

            // Will either be this 'Final' subdirectory or the landscape directory.
            OutputFinal = Path.Combine(Output, "Final");

            AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LandscapeBuilder");
        }

        public void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path.Combine(AppData, "settings.conf"), json);
        }

        public void CreateDirectories()
        {
            // Create all the output directories if they don't already exist.
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(OutputTexture);
            Directory.CreateDirectory(OutputTexturePatch);
            Directory.CreateDirectory(OutputForestMapTiles);
            Directory.CreateDirectory(OutputThermalMap);
            Directory.CreateDirectory(OutputThermalMapTiles);

            Directory.CreateDirectory(OutputFinal);
            Directory.CreateDirectory(OutputDDS);
            Directory.CreateDirectory(OutputForestMap);
            Directory.CreateDirectory(OutputHeightMap);

            Directory.CreateDirectory(AppData);
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
                        CondorLandscape = Path.Combine(key.GetValue("InstallLocation").ToString(), "Landscapes");
                        // If the landscape name hasn't been set yet, pick one as the default.
                        if (LandscapeName == null)
                        {
                            string landscapePath = Directory.GetDirectories(CondorLandscape).FirstOrDefault();
                            LandscapeName = new DirectoryInfo(landscapePath).Name;
                        }
                        break;
                    }
                }
            }
        }
    }
}
