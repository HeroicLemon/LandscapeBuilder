using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace LandscapeBuilderLib
{
    public abstract class LandData
    {
        public Color ThermalColor { get; set; } = Color.Black;

        public ForestType ForestType { get; set; } = ForestType.None;

        public string Description { get; set; }

        public bool IsDefault { get; set; }

        // For the forest colors, black is no forest and white is forest.
        [JsonIgnore]
        public Color DeciduousForestColor
        {
            get
            {
                return ForestType == ForestType.Mixed || ForestType == ForestType.Deciduous ? Color.White : Color.Black;
            }
        }
        [JsonIgnore]
        public Color ConiferousForestColor
        {
            get
            {
                return ForestType == ForestType.Mixed || ForestType == ForestType.Coniferous ? Color.White : Color.Black;
            }
        }

        public bool IsWater { get; set; }

        public LandData(Color thermalColor, string description, ForestType forestType, bool isWater, bool isDefault)
        {
            ThermalColor = thermalColor;
            ForestType = forestType;
            IsWater = isWater;
            Description = description;
            IsDefault = isDefault;
        }

        public abstract Color GetColor(int i, int j);

        // If this data is for water, set the alpha channel to 0.
        protected Color AdjustAlphaIfWater(Color currentColor)
        {
            Color newColor = currentColor;
            if (IsWater)
            {
                newColor = Color.FromArgb(0x00, currentColor.R, currentColor.G, currentColor.B);
            }

            return newColor;
        }
    }

    // This is for actual textures
    public class TexturedLandData : LandData
    {
        [JsonIgnore]
        public BitmapWrapper Texture { get; set; }

        public string Path { get; set; }

        public TexturedLandData(string path, Color thermalColor, string description = "", ForestType forestType = ForestType.None, bool isWater = false, bool isDefault = false) : base(thermalColor, description, forestType, isWater, isDefault)
        {
            Path = path;
            // TODO: Need to handle case where textures do not load properly.
            if (File.Exists(Path))
            {
                Texture = new BitmapWrapper(path);
            }
        }

        public override Color GetColor(int i, int j)
        {
            // TODO: Improve error handling here instead of just outputting white.
            Color color = Color.White;
            if(Texture != null)
            {
                color = Texture.GetPixel(i, j);
                color = AdjustAlphaIfWater(color);
            }

            return color;
        }
    }

    // This is for those landscape objects that we want to just draw as a particular color (e.g, roads)
    public class ColoredLandData : LandData
    {
        public Color Color { get; set; }

        public ColoredLandData(Color color, Color thermalColor, string description = "", ForestType forestType = ForestType.None, bool isWater = false, bool isDefault = false) : base(thermalColor, description, forestType, isWater, isDefault)
        {
            Color = color;
        }

        public override Color GetColor(int i, int j)
        {
            return AdjustAlphaIfWater(Color);
        }
    }
}
