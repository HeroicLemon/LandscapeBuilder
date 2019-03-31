using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace LandscapeBuilderLib
{
    public sealed class Airport
    {
        private enum Offset
        {
            NameLength = 0x00,
            Name = 0x01,
            Latitude = 0x24,
            Longitude = 0x28,
            Altitude = 0x2C,
            Direction = 0x30,
            Length = 0x34,
            Width = 0x38,
            Asphalt = 0x3C,
            Frequency = 0x40,
            PrimaryDirectionReversed = 0x44,
            TowPrimaryLeftSide = 0x45,
            TowSecondaryLeftSide = 0x46
        }

        public string Name { get; private set; }
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        public float Altitude { get; private set; }
        public int Direction { get; private set; }
        public int Length { get; private set; }
        public int Width { get; private set; }
        public bool Asphalt { get; private set; }
        public float Frequency { get; private set; }
        public bool PrimaryDirectionReversed { get; private set; }
        public bool TowPrimaryLeftSide { get; private set; }
        public bool TowSecondaryLeftSide { get; private set; }

        // The lat/long of the four corners of the runway. Used for flattening the terrain around the runway.
        public PointF[] RunwayCorners { get; private set; }

        // Objects for writing .obj and .mtl files
        ObjFile objFileG = new ObjFile();
        MtlFile mtlFileG = new MtlFile();
        ObjFile objFileO = new ObjFile();
        MtlFile mtlFileO = new MtlFile();

        public Airport(string name, float latitude, float longitude, float altitude, int direction, int length, int width, bool asphalt = false, PointF[] runwayCorners = null, float frequency = 123.3f, bool primaryDirectionReversed = false, bool towPrimaryLeftSide = false, bool towSecondaryLeftSide = false)
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Direction = direction;
            Length = length;
            Width = width;
            Frequency = frequency;
            Asphalt = asphalt;
            PrimaryDirectionReversed = primaryDirectionReversed;
            TowPrimaryLeftSide = towPrimaryLeftSide;
            TowSecondaryLeftSide = towSecondaryLeftSide;
            RunwayCorners = runwayCorners;
        }
        
        // Gets the bytes to be used to add the airport to the .apt file.
        public byte[] GetAptBytes()
        {
            byte[] airportBytes = new byte[0x48];

            copyIntoArray(airportBytes, BitConverter.GetBytes(Name.Length), Offset.NameLength);
            copyIntoArray(airportBytes, Encoding.UTF8.GetBytes(Name), Offset.Name);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Latitude), Offset.Latitude);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Longitude), Offset.Longitude);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Altitude), Offset.Altitude);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Direction), Offset.Direction);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Length), Offset.Length);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Width), Offset.Width);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Asphalt), Offset.Asphalt);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Frequency), Offset.Frequency);
            copyIntoArray(airportBytes, BitConverter.GetBytes(PrimaryDirectionReversed), Offset.PrimaryDirectionReversed);
            copyIntoArray(airportBytes, BitConverter.GetBytes(TowPrimaryLeftSide), Offset.TowPrimaryLeftSide);
            copyIntoArray(airportBytes, BitConverter.GetBytes(TowSecondaryLeftSide), Offset.TowSecondaryLeftSide);

            return airportBytes;
        }

        public void GenerateObjectFiles()
        {
            generateGFiles();
            generateOFiles();
        }

        // Generates the <airport name>G.obj and <airport name>G.mtl files for the grass/asphalt and paint objects.
        private void generateGFiles()
        {
            // Some of this stuff is currently hardcoded based on the object files I've created in blender and on the outputs from JBr's AirportMaker tool.
            string materialName = "01_Default";

            objFileG.AddMtlLib(string.Format("{0}G", Name));
            objFileG.AddNewLine();
            objFileG.AddObject(Asphalt ? "Asphalt" : "Grass");
            objFileG.AddNewLine();
            float x = Length / 2.0f;
            float y = 0.001f;
            float z = Width / 2.0f;

            objFileG.AddVertexCoordinate(new Vertex(-x, y, z));
            objFileG.AddVertexCoordinate(new Vertex(x, y, z));
            objFileG.AddVertexCoordinate(new Vertex(x, y, -z));
            objFileG.AddVertexCoordinate(new Vertex(-x, y, -z));
            objFileG.AddNewLine();

            objFileG.AddVertexNormal(0, 1, -0);
            objFileG.AddNewLine();

            objFileG.AddTextureCoordinate(0, 0, 0);
            objFileG.AddTextureCoordinate(0.8, 0, 0);
            objFileG.AddTextureCoordinate(0.8, 0.12, 0);
            objFileG.AddTextureCoordinate(0, 0.12, 0);
            objFileG.AddNewLine();

            objFileG.AddUseMtl(materialName);
            objFileG.AddSmoothing(true);

            FaceData[] data = new FaceData[3];
            data[0] = new FaceData(1, 1, 1);
            data[1] = new FaceData(2, 2, 1);
            data[2] = new FaceData(3, 3, 1);
            objFileG.AddFace(data);

            data[0] = new FaceData(3, 3, 1);
            data[1] = new FaceData(4, 4, 1);
            data[2] = new FaceData(1, 1, 1);
            objFileG.AddFace(data);

            // Now the .mtl file
            mtlFileG.AddNewMtl(materialName);
            mtlFileG.AddSpecularExponent(10);
            mtlFileG.AddOpticalDensity(1.5);
            mtlFileG.AddDisolved(1);
            mtlFileG.AddTransmissionFilter(1, 1, 1);
            mtlFileG.AddIlluminationModel(2);
            mtlFileG.AddAmbientColor(1, 1, 1);
            mtlFileG.AddDiffuseColor(1, 1, 1);
            mtlFileG.AddSpecularColor(0, 0, 0);

            objFileG.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsObjDir, string.Format("{0}G.obj", Name)));
            mtlFileG.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsObjDir, string.Format("{0}G.mtl", Name)));
        }

        // Generates the <airport name>O.obj and <airport name>O.mtl files for the 3D objects.
        private void generateOFiles()
        {
            // Currently just generates a windsock halfway down the runway.
            objFileO.AddMtlLib(string.Format("{0}O", Name));
            objFileO.AddNewLine();
            objFileO.AddWindsackPoleVertices(Width / 2f + 5);
            objFileO.AddWindsockPoleStaticInfo();
            objFileO.AddNewLine();
            objFileO.AddWindsackVertices(Width / 2f + 5);
            objFileO.AddWindsackStaticInfo();

            mtlFileO.AddWindsockMtl();

            objFileO.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsObjDir, string.Format("{0}O.obj", Name)));
            mtlFileO.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsObjDir, string.Format("{0}O.mtl", Name)));
        }

        private void copyIntoArray(byte[] destinationArray, byte[] sourceArray, Offset offset)
        {
            int o = (int)offset;
            Array.Copy(sourceArray, 0, destinationArray, (int)offset, sourceArray.Length);
        }
    }
}
