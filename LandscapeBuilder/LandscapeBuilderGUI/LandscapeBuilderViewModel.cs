using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LandscapeBuilderLib;
using Prism.Mvvm;
using Prism.Commands;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace LandscapeBuilderGUI
{
    public class LandscapeBuilderViewModel : BindableBase
    {
        LandscapeBuilder _landscapeBuilder = new LandscapeBuilder(false);

        #region Properties
        private Dictionary<System.Drawing.Color, LandData> _textures;
        public Dictionary<System.Drawing.Color, LandData> Textures
        {
            get { return _textures; }
            set { SetProperty(ref _textures, value); }
        }

        private string _outputDirectory;
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { SetProperty(ref _outputDirectory, value); }
        }

        private string _inputDirectory;
        public string InputDirectory
        {
            get { return _inputDirectory; }
            set
            {
                SetProperty(ref _inputDirectory, value);
                populateTileNames();
            }
        }

        private bool _genDDS = true;
        public bool GenDDS
        {
            get { return _genDDS; }
            set { SetProperty(ref _genDDS, value); }
        }

        private bool _genForestFiles = true;
        public bool GenForestFiles
        {
            get { return _genForestFiles; }
            set { SetProperty(ref _genForestFiles, value); }
        }

        private bool _genThermalFile = true;
        public bool GenThermalFile
        {
            get { return _genThermalFile; }
            set { SetProperty(ref _genThermalFile, value); }
        }

        private bool _outputToCondor = false;
        public bool OutputToCondor
        {
            get { return _outputToCondor; }
            set { SetProperty(ref _outputToCondor, value); }
        }

        private int _landscapeWidth = 0;
        public int LandscapeWidth
        {
            get { return _landscapeWidth; }
            set
            {
                SetProperty(ref _landscapeWidth, value);
                updateQGISString(_landscapeWidth, _landscapeHeight);
            }
        }

        private int _landscapeHeight = 0;
        public int LandscapeHeight
        {
            get { return _landscapeHeight; }
            set
            {
                SetProperty(ref _landscapeHeight, value);
                updateQGISString(_landscapeWidth, _landscapeHeight);
            }
        }

        private ObservableCollection<string> _landscapeNames = new ObservableCollection<string>();
        public ObservableCollection<string> LandscapeNames
        {
            get { return _landscapeNames; }
            set { SetProperty(ref _landscapeNames, value); }
        }

        private string _landscapeName;
        public string LandscapeName
        {
            get { return _landscapeName; }
            set
            {
                SetProperty(ref _landscapeName, value);
                SettingsManager.Instance.LandscapeName = value;
            }
        }

        private ObservableCollection<string> _tileNames = new ObservableCollection<string>();
        public ObservableCollection<string> TileNames
        {
            get { return _tileNames; }
            set { SetProperty(ref _tileNames, value); }
        }

        private bool _inputTilesPresent = false;
        public bool InputTilesPresent
        {
            get { return _inputTilesPresent; }
            set
            {
                SetProperty(ref _inputTilesPresent, value);
                RunCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _generateSingleTile;
        public bool GenerateSingleTile
        {
            get { return _generateSingleTile && InputTilesPresent; }
            set { SetProperty(ref _generateSingleTile, value); }
        }

        private string _singleTileName;
        public string SingleTileName
        {
            get { return _singleTileName; }
            set { SetProperty(ref _singleTileName, value); }
        }

        private string _qgisString = string.Empty;
        public string QGISString
        {
            get { return _qgisString; }
            set { SetProperty(ref _qgisString, value); }
        }

        private string _progressText = string.Empty;
        public string ProgressText
        {
            get { return _progressText; }
            set { SetProperty(ref _progressText, value); }
        }

        private bool _builderRunning = false;
        private bool BuilderRunning
        {
            get { return _builderRunning; }
            set
            {
                _builderRunning = value;
                RunCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        public LandscapeBuilderViewModel()
        {
            RunCommand = new DelegateCommand(Run, CanRun);
            SaveCommand = new DelegateCommand(Save, CanSave);
            ChooseTextureCommand = new DelegateCommand<System.Drawing.Color?>(ChooseTexture);
            ChooseDirectoryCommand = new DelegateCommand<bool?>(ChooseDirectory);
            AddTextureCommand = new DelegateCommand<bool?>(AddTexture);
            DeleteTextureCommand = new DelegateCommand<System.Drawing.Color?>(DeleteTexture);
            ChangeDefaultCommand = new DelegateCommand<System.Drawing.Color?>(ChangeDefault);

            _landscapeBuilder.InitializeTextures();
            _textures = _landscapeBuilder.Textures;
            _outputDirectory = SettingsManager.Instance.OutputDir;
            _inputDirectory = SettingsManager.Instance.InputDir;
            _landscapeName = SettingsManager.Instance.LandscapeName;

            populateTileNames();
            populateLandscapes();
        }

        #region Commands
        public void MapColorChanged(System.Windows.Media.Color oldColor, System.Windows.Media.Color newColor)
        {
            System.Drawing.Color oldDrawingColor = System.Drawing.Color.FromArgb(oldColor.A, oldColor.R, oldColor.G, oldColor.B);
            System.Drawing.Color newDrawingColor = System.Drawing.Color.FromArgb(newColor.A, newColor.R, newColor.G, newColor.B);

            if (!Textures.ContainsKey(newDrawingColor))
            {                

                Dictionary<System.Drawing.Color, LandData> textureCopy = new Dictionary<System.Drawing.Color, LandData>(_textures);
                LandData data = textureCopy[oldDrawingColor];
                textureCopy.Remove(oldDrawingColor);
                textureCopy.Add(newDrawingColor, data);

                Textures = textureCopy;
            }
            else
            {
                MessageBox.Show("That color is already being used by another texture! Please choose another color.");
            }
        }

        public DelegateCommand SaveCommand { get; private set; }
        void Save()
        {
            _landscapeBuilder.Textures = Textures;
            _landscapeBuilder.SaveTextures();

            SettingsManager.Instance.OutputDir = OutputDirectory;
            SettingsManager.Instance.InputDir = InputDirectory;
            _landscapeBuilder.SaveSettings();
        }

        bool CanSave()
        {
            return !BuilderRunning;
        }

        public DelegateCommand RunCommand { get; private set; }
        async void Run()
        {
            Save();
            ProgressText = string.Empty;
            Task runBuilderTask = RunBuilderAsync();
            Task readConsoleTask = ReadProgressAsync();

            await Task.WhenAll(runBuilderTask, readConsoleTask);
        }

        bool CanRun()
        {
            return InputTilesPresent && !BuilderRunning;
        }

        async Task RunBuilderAsync()
        {
            string singleTile = GenerateSingleTile ? SingleTileName : string.Empty;
            BuilderRunning = true;
            await Task.Run((()
                => _landscapeBuilder.Build(GenDDS, GenForestFiles, GenThermalFile, OutputToCondor, SettingsManager.Instance.OutputDir, SettingsManager.Instance.InputDir, singleTile)
                ));
            BuilderRunning = false;
        }

        async Task ReadProgressAsync()
        {
            await Task.Run(() => ReadProgress());
        }

        void ReadProgress()
        {
            while(BuilderRunning)
            {
                if(_landscapeBuilder.OutputText != null && ProgressText != _landscapeBuilder.OutputText)
                {
                    ProgressText = _landscapeBuilder.OutputText;
                }
            }
        }

        public DelegateCommand<bool?> ChooseDirectoryCommand { get; private set; }
        void ChooseDirectory(bool? output)
        {
            if (output.HasValue)
            {
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.SelectedPath = output.Value ? OutputDirectory : InputDirectory;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    if (output.Value)
                    {
                        OutputDirectory = folderBrowserDialog.SelectedPath;
                    }
                    else
                    {
                        InputDirectory = folderBrowserDialog.SelectedPath;
                        populateTileNames();
                    }
                }
            }
        }

        public DelegateCommand<System.Drawing.Color?> ChooseTextureCommand { get; private set; }
        void ChooseTexture(System.Drawing.Color? keyToUpdate)
        {
            if (keyToUpdate.HasValue)
            {
                if (Textures[keyToUpdate.Value] is TexturedLandData)
                {
                    Dictionary<System.Drawing.Color, LandData> textureCopy = new Dictionary<System.Drawing.Color, LandData>(_textures);
                    TexturedLandData landData = textureCopy[keyToUpdate.Value] as TexturedLandData;

                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Filter = "Image Files(*.BMP; *.JPG; *.GIF; *.PNG)| *.BMP; *.JPG; *.GIF; *.PNG| All files(*.*) | *.*";
                    dialog.InitialDirectory = Path.GetDirectoryName(landData.Path);

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        landData.Path = dialog.FileName;
                    }

                    Textures = textureCopy;
                }
            }
        }

        public DelegateCommand<bool?> AddTextureCommand { get; private set; }
        void AddTexture(bool? texture)
        {
            if(texture.HasValue)
            {
                System.Drawing.Color mapColor = System.Drawing.Color.FromArgb(0xff, 0x0, 0x0, 0x0);
                int color = 0;
                // Get a color that hasn't been used yet for the default.
                while(Textures.ContainsKey(mapColor))
                {
                    color = mapColor.ToArgb();
                    mapColor = System.Drawing.Color.FromArgb(++color);
                }

                LandData landData;
                if(texture.Value)
                {
                    // Just use the first texture path we find
                    string path = string.Empty;
                    foreach(var data in Textures)
                    {
                        if(data.Value is TexturedLandData)
                        {
                            path = ((TexturedLandData)data.Value).Path;
                            break;
                        }
                    }

                    landData = new TexturedLandData(path, System.Drawing.Color.Black);
                }
                else
                {
                    landData = new ColoredLandData(System.Drawing.Color.Black, System.Drawing.Color.Black);
                }

                Dictionary<System.Drawing.Color, LandData> textureCopy = new Dictionary<System.Drawing.Color, LandData>(_textures);
                textureCopy.Add(mapColor, landData);

                Textures = textureCopy;
            }
        }


        public DelegateCommand<System.Drawing.Color?> DeleteTextureCommand { get; private set; }
        void DeleteTexture(System.Drawing.Color? keyToDelete)
        {
            if(keyToDelete.HasValue)
            {
                Dictionary<System.Drawing.Color, LandData> textureCopy = new Dictionary<System.Drawing.Color, LandData>(_textures);
                textureCopy.Remove(keyToDelete.Value);

                Textures = textureCopy;
            }
        }

        public DelegateCommand<System.Drawing.Color?> ChangeDefaultCommand { get; private set; }
        void ChangeDefault(System.Drawing.Color? keyNewDefault)
        {
            if(keyNewDefault.HasValue)
            {
                Dictionary<System.Drawing.Color, LandData> textureCopy = new Dictionary<System.Drawing.Color, LandData>(_textures);
                foreach(var texture in textureCopy)
                {
                    texture.Value.IsDefault = keyNewDefault == texture.Key;
                }

                Textures = textureCopy;
            }
        }
        #endregion

        private void populateLandscapes()
        {
            LandscapeNames.Clear();

            if (SettingsManager.Instance.CondorLandscapesDir != null)
            {
                string[] landscapes = Directory.GetDirectories(SettingsManager.Instance.CondorLandscapesDir);
                foreach (string landscape in landscapes)
                {
                    LandscapeNames.Add(landscape.Substring(SettingsManager.Instance.CondorLandscapesDir.Length + 1));
                }
            }
        }

        private void populateTileNames()
        {
            TileNames.Clear();

            if (Directory.Exists(SettingsManager.Instance.InputAtlasDir))
            {
                string[] mapFiles = Directory.GetFiles(SettingsManager.Instance.InputAtlasDir, "*.png", SearchOption.TopDirectoryOnly);
                foreach (string file in mapFiles)
                {
                    TileNames.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            InputTilesPresent = TileNames.Count > 0;
        }

        private void updateQGISString(int width, int height)
        {
            if(width > 0 && height > 0)
            {
                QGISString = Utilities.GetAtlasString(width, height);
            }
            else
            {
                QGISString = string.Empty;
            }
        }

    }
}
