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
            FileText += string.Format("# {0}\n", comment);
        }

        public void AddNewLine(int lines = 1)
        {
            for (int i = 0; i < lines; i++)
            {
                FileText += "\n";
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

        public void AddVertexCoordinate(double x, double y, double z)
        {
            FileText += string.Format("v {0:0.0000} {1:0.0000} {2:0.0000}\n", x, y, z);
        }

        public void AddTextureCoordinate(double u, double v = 0, double w = 0)
        {
            FileText += string.Format("vt {0:0.0000} {1:0.0000} {2:0.0000}\n", u, v, w);
        }

        public void AddVertexNormal(double x, double y, double z)
        {
            FileText += string.Format("vn {0:0.0000} {1:0.0000} {2:0.0000}\n", x, y, z);
        }

        public void AddFace(FaceData[] faceData)
        {
            string faceString = string.Empty;
            foreach(FaceData data in faceData)
            {
                faceString += string.Format(" {0}", data.ToString());
            }

            FileText += string.Format("f{0}\n", faceString);
        }

        public void AddObject(string obj)
        {
            FileText += string.Format("o {0}\n", obj);
        }

        public void AddGroup(string group)
        {
            FileText += string.Format("g {0}\n", group);
        }

        public void AddMtlLib(string libName)
        {
            FileText += string.Format("mtllib {0}.mtl\n", libName);
        }

        public void AddUseMtl(string mtl)
        {
            FileText += string.Format("usemtl {0}\n", mtl);
        }

        public void AddSmoothing(bool smoothingOn)
        {
            FileText += string.Format("s{0}\n", smoothingOn ? "1" : " off");
        }
    }

    public class MtlFile : WavefrontFile
    {
        public MtlFile() : base()
        {

        }       

        public void AddNewMtl(string newMtl)
        {
            FileText += string.Format("newmtl {0}\n", newMtl);
        }
        
        public void AddOpticalDensity(double ni)
        {
            FileText += string.Format("Ni {0:0.0000}\n", ni);
        }

        public void AddDisolved(double d)
        {
            // Some implementations use d (disolved) where 1 is opaque and some use Tr (transparent) where 0 is opaque.
            FileText += string.Format("d {0:0.0000}\n", d);
            FileText += string.Format("Tr {0:0.0000}\n", 1 - d);
        }

        public void AddTransmissionFilter(double r, double g, double b)
        {
            FileText += string.Format("Tf {0:0.0000} {1:0.0000} {2:0.0000}\n", r, g, b);
        }

        public void AddIlluminationModel(int illuminationModel)
        {
            FileText += string.Format("illum {0}\n", illuminationModel);
        }

        public void AddAmbientColor(double r, double g, double b)
        {
            FileText += string.Format("Ka {0:0.0000} {1:0.0000} {2:0.0000}\n", r, g, b);
        }

        public void AddDiffuseColor(double r, double g, double b)
        {
            FileText += string.Format("Kd {0:0.0000} {1:0.0000} {2:0.0000}\n", r, g, b);
        }

        public void AddSpecularColor(double r, double g, double b)
        {
            FileText += string.Format("Ks {0:0.0000} {1:0.0000} {2:0.0000}\n", r, g, b);
        }

        public void AddSpecularExponent(double ns)
        {
            FileText += string.Format("Ns {0:0.0000}\n", ns);
        }
    }

    public struct FaceData
    {
        int Vertex { get; set; }
        int Texture { get; set; }
        int Normal { get; set; }

        public FaceData(int vertex, int texture, int normal)
        {
            Vertex = vertex;
            Texture = texture;
            Normal = normal;
        }

        public override string ToString()
        {
            return string.Format(@"{0}/{1}/{2}", Vertex, Texture, Normal);
        }
    }
}
