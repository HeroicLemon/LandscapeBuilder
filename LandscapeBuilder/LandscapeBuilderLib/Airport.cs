using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LandscapeBuilderLib
{
    public sealed partial class Airport
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
        public float Direction { get; private set; }

        // V2 of the landscape editor supports direction headings to .01 degrees, but it's still an integer with a special format.
        public int DirectionOutput 
        { 
            get 
            {
                string formatted = Direction.ToString("0.00");
                string[] parts = formatted.Split('.');

                int whole = int.Parse(parts[0]);
                int fractional = int.Parse(parts[1]);

                return 100000 + (whole * 100) + fractional;
            } 
        }
        public int Length { get; private set; }
        public int Width { get; private set; }
        public bool Asphalt { get; private set; }
        public float Frequency { get; private set; }
        public bool PrimaryDirectionReversed { get; private set; }
        public bool TowPrimaryLeftSide { get; private set; }
        public bool TowSecondaryLeftSide { get; private set; }

        // The lat/long of the four corners of the runway. Used for flattening the terrain around the runway.
        public PointF[] RunwayCorners { get; private set; }

        public Airport(string name, float latitude, float longitude, float altitude, float direction, int length, int width, bool asphalt = false, PointF[] runwayCorners = null, float frequency = 123.3f, bool primaryDirectionReversed = false, bool towPrimaryLeftSide = false, bool towSecondaryLeftSide = false)
        {
            // TODO: Really only need to do this for the file names...
            Regex regex = new Regex(@"[<>:""/\\|?*]");
            Name = regex.Replace(name, "-");
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
            copyIntoArray(airportBytes, BitConverter.GetBytes(DirectionOutput), Offset.Direction);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Length), Offset.Length);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Width), Offset.Width);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Asphalt), Offset.Asphalt);
            copyIntoArray(airportBytes, BitConverter.GetBytes(Frequency), Offset.Frequency);
            copyIntoArray(airportBytes, BitConverter.GetBytes(PrimaryDirectionReversed), Offset.PrimaryDirectionReversed);
            copyIntoArray(airportBytes, BitConverter.GetBytes(TowPrimaryLeftSide), Offset.TowPrimaryLeftSide);
            copyIntoArray(airportBytes, BitConverter.GetBytes(TowSecondaryLeftSide), Offset.TowSecondaryLeftSide);

            return airportBytes;
        }

        private void copyIntoArray(byte[] destinationArray, byte[] sourceArray, Offset offset)
        {
            int o = (int)offset;
            Array.Copy(sourceArray, 0, destinationArray, (int)offset, sourceArray.Length);
        }
    }
}
