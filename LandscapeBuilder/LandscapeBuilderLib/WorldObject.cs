using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LandscapeBuilderLib
{
    // Represents a 3D object in Condor. Will be used for generating the .obj and .c3d files.
    public class WorldObject
    {
        public string Name { get; set; }
        public List<Vertex> Vertices { get; set; } = new List<Vertex>();
        public List<Vertex> TextureCoordinates { get; set; } = new List<Vertex>();
        public List<Vertex> VertexNormals { get; set; } = new List<Vertex>();
        public List<Face> Faces { get; set; } = new List<Face>();
    }

    public class Material : IEquatable<Material>
    {
        public string Name { get; set; }
        public float OpticalDensity { get; set; }
        public float Dissolved { get; set; }
        public MtlColor TransmissionFilter { get; set; }
        public int IlluminationModel { get; set; }
        public MtlColor AmbientColor { get; set; }
        public MtlColor DiffuseColor { get; set; }
        public MtlColor SpecularColor { get; set; }
        public float SpecularExponent { get; set; }

        public bool Equals(Material other)
        {
            return Name == other.Name &&
                OpticalDensity == other.OpticalDensity &&
                Dissolved == other.Dissolved &&
                TransmissionFilter == other.TransmissionFilter &&
                IlluminationModel == other.IlluminationModel &&
                AmbientColor == other.AmbientColor &&
                DiffuseColor == other.DiffuseColor &&
                SpecularColor == other.SpecularColor &&
                SpecularExponent == other.SpecularExponent;
        }

        public override int GetHashCode()
        {
            return (Name, OpticalDensity, Dissolved, TransmissionFilter, IlluminationModel,
                AmbientColor, DiffuseColor, SpecularColor, SpecularExponent).GetHashCode();
        }
    }

    // Dictionary wrapper to map WorldObjects to their Materials
    public class WorldObjectList : Dictionary<Material, List<WorldObject>>
    {
        public void Add(Material material, WorldObject obj)
        {
            if(ContainsKey(material))
            {
                this[material].Add(obj);
            }
            else
            {
                Add(material, new List<WorldObject>() { obj });
            }
        }

        // Writes this WorldObjectlist to the .obj and .mtl files.
        public void ToObjMtl(string fileName)
        {
            ObjFile objFile = new ObjFile();
            objFile.AddMtlLib(fileName);
            objFile.AddNewLine();

            MtlFile mtlFile = new MtlFile();

            foreach (var kvp in this)
            {
                // Write the material
                Material material = kvp.Key;
                mtlFile.AddNewMtl(material.Name);
                mtlFile.AddSpecularExponent(material.SpecularExponent);
                mtlFile.AddOpticalDensity(material.OpticalDensity);
                mtlFile.AddDissolved(material.Dissolved);
                mtlFile.AddTransmissionFilter(material.TransmissionFilter);
                mtlFile.AddIlluminationModel(material.IlluminationModel);
                mtlFile.AddAmbientColor(material.AmbientColor);
                mtlFile.AddDiffuseColor(material.DiffuseColor);
                mtlFile.AddSpecularColor(material.SpecularColor);
                mtlFile.AddNewLine();

                // Write all the objects associated with this material
                foreach(WorldObject obj in kvp.Value)
                {
                    objFile.AddObject(obj.Name);
                    objFile.AddNewLine();

                    foreach(Vertex vertex in obj.Vertices)
                    {
                        objFile.AddVertexCoordinate(vertex);
                    }
                    objFile.AddNewLine();

                    foreach(Vertex vertex in obj.VertexNormals)
                    {
                        objFile.AddVertexNormal(vertex);
                    }
                    objFile.AddNewLine();

                    foreach(Vertex vertex in obj.TextureCoordinates)
                    {
                        objFile.AddTextureCoordinate(vertex);
                    }
                    objFile.AddNewLine();

                    objFile.AddUseMtl(material.Name);
                    // TODO: Check on smoothing
                    objFile.AddSmoothing(true);

                    foreach(Face face in obj.Faces)
                    {
                        objFile.AddFace(face);
                    }
                    objFile.AddNewLine();
                }
            }

            objFile.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsObjDir, fileName));
            mtlFile.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsObjDir, fileName));
        }
    }

    public class Vertex
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vertex(float x = 0, float y = 0, float z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", X, Y, Z);
        }
    }

    public class Face : List<FaceDatum>
    {
        public override string ToString()
        {
            string data = string.Empty;
            foreach(FaceDatum datum in this)
            {
                data += string.Format("{0} ", datum.ToString());
            }

            return data.TrimEnd(' ');
        }
    }

    public class FaceDatum
    {
        int VertexIndex { get; set; }
        int TextureIndex { get; set; }
        int NormalIndex { get; set; }

        public FaceDatum(int vertexIndex = 1, int textureIndex = 1, int normalIndex = 1)
        {
            VertexIndex = vertexIndex;
            TextureIndex = textureIndex;
            NormalIndex = normalIndex;
        }

        public override string ToString()
        {
            // Texture and normal are optional.
            string texture = TextureIndex == 0 ? string.Empty : TextureIndex.ToString();
            string normal = NormalIndex == 0 ? string.Empty : NormalIndex.ToString();
            return string.Format(@"{0}/{1}/{2}", VertexIndex, texture, normal);
        }
    }

    public class MtlColor
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }

        public MtlColor(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}
