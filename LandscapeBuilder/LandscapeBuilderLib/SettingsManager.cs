using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace LandscapeBuilderLib
{
    [JsonObject(MemberSerialization.OptIn)]
    sealed class SettingsManager
    {
        #region Directories
        // The input directory where the reference maps are stored.
        [JsonProperty]
        public string InputMap { get; set; }

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

        // Local app data for storing settings.
        public string AppData { get; private set; }

        // Directory that Condor is installed to
        public string CondorLandscape { get; private set; }

        public string Executable { get; private set; }
        #endregion

        [JsonProperty]
        public string LandscapeName { get; set; }


        private static readonly Lazy<SettingsManager> lazy = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance {  get { return lazy.Value; } }

        private SettingsManager()
        {

        }

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
            this.InputMap = other.InputMap;
        }

        private void initializeDefaultDirectories()
        {

            Executable = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);

            // Inputs can be set from command line argumente.
            InputMap = Path.Combine(Executable, "Atlas");

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
