using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using DotSpatial.Data;

namespace LandscapeBuilderLib
{
    abstract class RunwayParser
    {
        public abstract List<Airport> Parse();
    }

    abstract class FAARunwayParser : RunwayParser
    {
        // Determines if the runway is asphalt based on the FAA COMP_CODE
        protected bool isAsphalt(string comp_code)
        {
            bool isAsphalt = false;

            switch (comp_code)
            {
                case "ASP+DIRT":
                case "ASP+GRASS":
                case "ASP+TRTD":
                case "ASPH":
                case "CONC":
                case "CONC+ASPH":
                case "CONC+GVL":
                case "CONC+TRTD":
                case "PSP":
                    {
                        isAsphalt = true;
                    }
                    break;
            }

            return isAsphalt;
        }

        protected float feetToMeters(float feet)
        {
            return feet / 3.2808f;
        }
    }

    // An initial attempt to parse the FAA runway and airport shapefiles to get the needed data.
    // Relies on DotSpatial library.
    class FAAShapefileRunwayParser : FAARunwayParser
    {
        public override List<Airport> Parse()
        {
            List<Airport> airports = new List<Airport>();

            string runwayShapeFilePath = @"Runways.shp";
            string airportShapeFilePath = @"Airports.shp";

            PolygonShapefile runwayShapefile = new PolygonShapefile(runwayShapeFilePath);
            PointShapefile airportShapefile = new PointShapefile(airportShapeFilePath);

            // It'd be nice to limit the airports to just those within the extents of the landscape, but I don't think that's possible as lat/long are text fields...
            List<IFeature> airportFeatures = airportShapefile.SelectByAttribute("STATE='VA' AND TYPE_CODE='AD'");
            foreach (Feature airportFeature in airportFeatures)
            {
                string name = (string)airportFeature.DataRow["NAME"];

                string airportId = (string)airportFeature.DataRow["GLOBAL_ID"];
                // For now just get the runway that happens to be first.
                IFeature runwayFeature = runwayShapefile.SelectByAttribute(string.Format("AIRPORT_ID='{0}'", airportId)).FirstOrDefault();

                // Get the latitude and longitude of the runway from it's coordinates.
                PointF coordinates = getLatLongFromCorners(runwayFeature.Coordinates);
                float latitude = coordinates.Y;
                float longitude = coordinates.X;

                float altitude = feetToMeters(float.Parse(airportFeature.DataRow["ELEVATION"].ToString()));

                int direction = getDirectionFromCorners(runwayFeature.Coordinates);
                int length = (int)Math.Round(feetToMeters(float.Parse(runwayFeature.DataRow["LENGTH"].ToString())));
                int width = (int)Math.Round(feetToMeters(float.Parse(runwayFeature.DataRow["WIDTH"].ToString())));

                bool asphalt = isAsphalt((string)runwayFeature.DataRow["COMP_CODE"]);

                airports.Add(new Airport(name, latitude, longitude, altitude, direction, length, width, asphalt));
            }

            return airports;
        }

        // This mostly works, but I think I need to account for magnetic variation.
        private int getDirectionFromCorners(IList<DotSpatial.Topology.Coordinate> corners)
        {
            int direction;

            direction = (int)Math.Round(Math.Atan2(corners[0].X - corners[3].X, corners[0].Y - corners[3].Y) * (180 / Math.PI));

            if (direction < 0)
            {
                direction = direction + 360;
            }

            return direction;
        }

        private PointF getLatLongFromCorners(IList<DotSpatial.Topology.Coordinate> corners)
        {
            // Get the average coordinates from two opposite corners.
            float latitude = (float)((corners[0].Y + corners[2].Y) / 2);
            float longitude = (float)((corners[0].X + corners[2].X) / 2);

            return new PointF(longitude, latitude);
        }
    }
}
