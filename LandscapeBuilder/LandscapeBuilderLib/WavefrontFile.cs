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

        public abstract void WriteFile(string path);
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

        public void AddTextureCoordinate(Vertex vertex)
        {
            FileText += string.Format("vt {0:0.0000} {1:0.0000} {2:0.0000}", vertex.X, vertex.Y, vertex.Z);
            AddNewLine();
        }

        public void AddVertexNormal(Vertex vertex)
        {
            FileText += string.Format("vn {0:0.0000} {1:0.0000} {2:0.0000}", vertex.X, vertex.Y, vertex.Z);
            AddNewLine();
        }

        public void AddFace(Face face)
        {
            FileText += string.Format("f {0}", face.ToString());
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

        public override void WriteFile(string path)
        {            
            path += ".obj";
            File.WriteAllText(path, FileText);
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

        public void AddDissolved(double d)
        {
            // Some implementations use d (disolved) where 1 is opaque and some use Tr (transparent) where 0 is opaque.
            FileText += string.Format("d {0:0.0000}", d);
            AddNewLine();
            FileText += string.Format("Tr {0:0.0000}", 1 - d);
            AddNewLine();
        }

        public void AddTransmissionFilter(MtlColor color)
        {
            if (color != null)
            {
                FileText += string.Format("Tf {0:0.0000} {1:0.0000} {2:0.0000}", color.R, color.G, color.B);
                AddNewLine();
            }
        }

        public void AddIlluminationModel(int illuminationModel)
        {
            FileText += string.Format("illum {0}", illuminationModel);
            AddNewLine();
        }

        public void AddAmbientColor(MtlColor color)
        {
            if (color != null)
            {
                FileText += string.Format("Ka {0:0.0000} {1:0.0000} {2:0.0000}", color.R, color.G, color.B);
                AddNewLine();
            }
        }

        public void AddDiffuseColor(MtlColor color)
        {
            if (color != null)
            {
                FileText += string.Format("Kd {0:0.0000} {1:0.0000} {2:0.0000}", color.R, color.G, color.B);
                AddNewLine();
            }
        }

        public void AddSpecularColor(MtlColor color)
        {
            if (color != null)
            {
                FileText += string.Format("Ks {0:0.0000} {1:0.0000} {2:0.0000}", color.R, color.G, color.B);
                AddNewLine();
            }
        }

        public void AddSpecularExponent(double ns)
        {
            FileText += string.Format("Ns {0:0.0000}", ns);
            AddNewLine();
        }

        public override void WriteFile(string path)
        {
            path += ".mtl";
            File.WriteAllText(path, FileText);
        }
    }
}
