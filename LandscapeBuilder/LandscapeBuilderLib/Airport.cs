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
        ObjFile objFile = new ObjFile();
        MtlFile mtlFile = new MtlFile();

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

        public void GenerateObjMtlFile()
        {
            // Some of this stuff is currently hardcoded based on the object files I've created in blender and on the outputs from JBr's AirportMaker tool.
            string materialName = "01_Default";

            objFile.AddMtlLib(string.Format("{0}G", Name));
            objFile.AddNewLine();
            objFile.AddObject(Asphalt ? "Asphalt" : "Grass");
            objFile.AddNewLine();
            double x = Length / 2.0;
            double y = 0.001;
            double z = Width / 2.0;

            objFile.AddVertexCoordinate(-x, y, z);
            objFile.AddVertexCoordinate(x, y, z);
            objFile.AddVertexCoordinate(x, y, -z);
            objFile.AddVertexCoordinate(-x, y, -z);
            objFile.AddNewLine();

            objFile.AddVertexNormal(0, 1, -0);
            objFile.AddNewLine();

            objFile.AddTextureCoordinate(0, 0, 0);
            objFile.AddTextureCoordinate(0.8, 0, 0);
            objFile.AddTextureCoordinate(0.8, 0.12, 0);
            objFile.AddTextureCoordinate(0, 0.12, 0);
            objFile.AddNewLine();

            objFile.AddUseMtl(materialName);
            objFile.AddSmoothing(true);

            FaceData[] data = new FaceData[3];
            data[0] = new FaceData(1, 1, 1);
            data[1] = new FaceData(2, 2, 1);
            data[2] = new FaceData(3, 3, 1);
            objFile.AddFace(data);

            data[0] = new FaceData(3, 3, 1);
            data[1] = new FaceData(4, 4, 1);
            data[2] = new FaceData(1, 1, 1);
            objFile.AddFace(data);

            // Now the .mtl file
            mtlFile.AddNewMtl(materialName);
            mtlFile.AddSpecularExponent(10);
            mtlFile.AddOpticalDensity(1.5);
            mtlFile.AddDisolved(1);
            mtlFile.AddTransmissionFilter(1, 1, 1);
            mtlFile.AddIlluminationModel(2);
            mtlFile.AddAmbientColor(1, 1, 1);
            mtlFile.AddDiffuseColor(1, 1, 1);
            mtlFile.AddSpecularColor(0, 0, 0);

            objFile.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsDir, string.Format("{0}G.obj", Name)));
            mtlFile.WriteFile(Path.Combine(SettingsManager.Instance.OutputAirportsDir, string.Format("{0}G.mtl", Name)));
        }

        private void copyIntoArray(byte[] destinationArray, byte[] sourceArray, Offset offset)
        {
            int o = (int)offset;
            Array.Copy(sourceArray, 0, destinationArray, (int)offset, sourceArray.Length);
        }
    }
}
