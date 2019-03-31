using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LandscapeBuilderLib
{
    // Used to create .obj and .mtl files for the 3D objects.
    public abstract class WavefrontFile
    {
        public string FileText { get; protected set; } = string.Empty;

        public WavefrontFile()
        {
            AddComment("Created by LandscapeBuilder");
            AddComment("https://github.com/HeroicLemon/LandscapeBuilder");
            AddNewLine(2);
        }

        public void AddComment(string comment)
        {
            FileText += string.Format("# {0}", comment);
            AddNewLine();
        }

        public void AddNewLine(int lines = 1)
        {
            for (int i = 0; i < lines; i++)
            {
                FileText += Environment.NewLine;
            }
        }

        public void WriteFile(string path)
        {
            File.WriteAllText(path, FileText);
        }
    }


    public class ObjFile : WavefrontFile
    {

        public ObjFile() : base()
        {

        }

        public void AddVertexCoordinate(Vertex vertex)
        {
            FileText += string.Format("v {0:0.0000} {1:0.0000} {2:0.0000}", vertex.X, vertex.Y, vertex.Z);
            AddNewLine();
        }

        public void AddTextureCoordinate(double u, double v = 0, double w = 0)
        {
            FileText += string.Format("vt {0:0.0000} {1:0.0000} {2:0.0000}", u, v, w);
            AddNewLine();
        }

        public void AddVertexNormal(double x, double y, double z)
        {
            FileText += string.Format("vn {0:0.0000} {1:0.0000} {2:0.0000}", x, y, z);
            AddNewLine();
        }

        public void AddFace(FaceData[] faceData)
        {
            string faceString = string.Empty;
            foreach(FaceData data in faceData)
            {
                faceString += string.Format(" {0}", data.ToString());
            }

            FileText += string.Format("f{0}", faceString);
            AddNewLine();
        }

        public void AddObject(string obj)
        {
            FileText += string.Format("o {0}", obj);
            AddNewLine();
        }

        public void AddGroup(string group)
        {
            FileText += string.Format("g {0}", group);
            AddNewLine();
        }

        public void AddMtlLib(string libName)
        {
            FileText += string.Format("mtllib {0}.mtl", libName);
            AddNewLine();
        }

        public void AddUseMtl(string mtl)
        {
            FileText += string.Format("usemtl {0}", mtl);
            AddNewLine();
        }

        public void AddSmoothing(bool smoothingOn)
        {
            FileText += string.Format("s{0}", smoothingOn ? "1" : " off");
            AddNewLine();
        }

        // Hardcode the windsock stuff for now.
        public void AddWindsackPoleVertices(float offsetX = 0, float offsetZ = 0)
        {
            string[] vertexStrings= Encoding.ASCII.GetString(Properties.Resources.WindsockOBJ, 0x42, 0x4D2).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            List<Vertex> vertices = new List<Vertex>();
            foreach(string vertex in vertexStrings)
            {
                string[] coordinates = vertex.Substring(2).Split(' ');
                float x = float.Parse(coordinates[0]);
                float y = float.Parse(coordinates[1]);
                float z = float.Parse(coordinates[2]);

                vertices.Add(new Vertex(x, y, z));
            }

            foreach(Vertex vertex in vertices)
            {
                Vertex tmp = vertex;
                tmp.X += offsetX;
                tmp.Z += offsetZ;
                AddVertexCoordinate(tmp);
            }

            AddNewLine();
        }

        public void AddWindsockPoleStaticInfo()
        {
            FileText += Encoding.ASCII.GetString(Properties.Resources.WindsockOBJ, 0x518, 0x124B);
            AddNewLine();
        }

        public void AddWindsackVertices(float offsetX = 0, float offsetZ = 0)
        {
            string[] vertexStrings = Encoding.ASCII.GetString(Properties.Resources.WindsockOBJ, 0x1767, 0x49).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            List<Vertex> vertices = new List<Vertex>();
            foreach (string vertex in vertexStrings)
            {
                string[] coordinates = vertex.Substring(2).Split(' ');
                float x = float.Parse(coordinates[0]);
                float y = float.Parse(coordinates[1]);
                float z = float.Parse(coordinates[2]);

                vertices.Add(new Vertex(x, y, z));
            }

            foreach (Vertex vertex in vertices)
            {
                Vertex tmp = vertex;
                tmp.X += offsetX;
                tmp.Z += offsetZ;
                AddVertexCoordinate(tmp);
            }

            AddNewLine();
        }

        public void AddWindsackStaticInfo()
        {
            FileText += Encoding.ASCII.GetString(Properties.Resources.WindsockOBJ, 0x17B4, 0x59);
            AddNewLine();
        }
    }

    public class MtlFile : WavefrontFile
    {
        public MtlFile() : base()
        {

        }       

        public void AddNewMtl(string newMtl)
        {
            FileText += string.Format("newmtl {0}", newMtl);
            AddNewLine();
        }
        
        public void AddOpticalDensity(double ni)
        {
            FileText += string.Format("Ni {0:0.0000}", ni);
            AddNewLine();
        }

        public void AddDisolved(double d)
        {
            // Some implementations use d (disolved) where 1 is opaque and some use Tr (transparent) where 0 is opaque.
            FileText += string.Format("d {0:0.0000}", d);
            AddNewLine();
            FileText += string.Format("Tr {0:0.0000}", 1 - d);
            AddNewLine();
        }

        public void AddTransmissionFilter(double r, double g, double b)
        {
            FileText += string.Format("Tf {0:0.0000} {1:0.0000} {2:0.0000}", r, g, b);
            AddNewLine();
        }

        public void AddIlluminationModel(int illuminationModel)
        {
            FileText += string.Format("illum {0}", illuminationModel);
            AddNewLine();
        }

        public void AddAmbientColor(double r, double g, double b)
        {
            FileText += string.Format("Ka {0:0.0000} {1:0.0000} {2:0.0000}", r, g, b);
            AddNewLine();
        }

        public void AddDiffuseColor(double r, double g, double b)
        {
            FileText += string.Format("Kd {0:0.0000} {1:0.0000} {2:0.0000}", r, g, b);
            AddNewLine();
        }

        public void AddSpecularColor(double r, double g, double b)
        {
            FileText += string.Format("Ks {0:0.0000} {1:0.0000} {2:0.0000}", r, g, b);
            AddNewLine();
        }

        public void AddSpecularExponent(double ns)
        {
            FileText += string.Format("Ns {0:0.0000}", ns);
            AddNewLine();
        }

        // Hardcode windsock stuff for now.
        public void AddWindsockMtl()
        {
            FileText += Encoding.ASCII.GetString(Properties.Resources.WindsockMTL);
            AddNewLine();
        }
    }

    public struct Vertex
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
    }

    public struct FaceData
    {
        int VertexIndex { get; set; }
        int TextureIndex { get; set; }
        int NormalIndex { get; set; }

        public FaceData(int vertexIndex = 1, int textureIndex = 1, int normalIndex = 1)
        {
            VertexIndex = vertexIndex;
            TextureIndex = textureIndex;
            NormalIndex = normalIndex;
        }

        public override string ToString()
        {
            return string.Format(@"{0}/{1}/{2}", VertexIndex, TextureIndex, NormalIndex);
        }
    }
}
