using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using DotSpatial.Data;
using DotSpatial.Topology;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.IO;

namespace LandscapeBuilderLib
{
    interface IAirportParser
    {
        List<Airport> Parse();
    }

    // Parses runway data primarily from FAA shapefiles for runways and airports. Accesses NASR database for direction.
    class FAAShapefileAirportParser : IAirportParser
    {
        public List<Airport> Parse()
        {
            List<Airport> airports = new List<Airport>();

            string runwayShapeFilePath = Path.Combine(SettingsManager.Instance.InputAirportDir, "Runways.shp");
            string airportShapeFilePath = Path.Combine(SettingsManager.Instance.InputAirportDir, "Airports.shp");

            if (File.Exists(runwayShapeFilePath) && File.Exists(airportShapeFilePath))
            {
                PolygonShapefile runwayShapefile = new PolygonShapefile(runwayShapeFilePath);
                PointShapefile airportShapefile = new PointShapefile(airportShapeFilePath);

                // It'd be nice to limit the airports to just those within the extents of the landscape, but I don't think that's possible as lat/long are text fields...
                // TODO: It may be possible to do that with the spatialite database.
                List<IFeature> airportFeatures = airportShapefile.SelectByAttribute("TYPE_CODE='AD'");
                foreach (Feature airportFeature in airportFeatures)
                {
                    DotSpatial.Topology.Point airportPoint = airportFeature.BasicGeometry as DotSpatial.Topology.Point;
                    if (coordinateWithinExtent(airportPoint.Coordinate))
                    {
                        string name = (string)airportFeature.DataRow["NAME"];

                        string airportId = (string)airportFeature.DataRow["GLOBAL_ID"];
                        // For now just get the runway that happens to be first.
                        List<IFeature> runwayFeatures = runwayShapefile.SelectByAttribute(string.Format("AIRPORT_ID='{0}'", airportId));
                        IFeature runwayFeature = runwayFeatures.FirstOrDefault();

                        Coordinate runwayCoordinate;
                        // For the case of a single runway, use the airport's coordinate.
                        if (runwayFeatures.Count == 1)
                        {
                            runwayCoordinate = airportPoint.Coordinate;
                        }
                        // Otherwise, use the four corners of the runway to determine the center lat/long of the runway. 
                        else
                        {
                            runwayCoordinate = getCenterLatLongFromCorners(runwayFeature.Coordinates);

                            // Some of the data in the shapefile is bad, with runways centered around 0, 0.
                            // If the result above is not within the extent, use the airport's coordinate and hope for the best.
                            if (!coordinateWithinExtent(runwayCoordinate))
                            {
                                runwayCoordinate = airportPoint.Coordinate;
                            }
                        }

                        float latitude = (float)runwayCoordinate.Y;
                        float longitude = (float)runwayCoordinate.X;
                        float altitude = Utilities.FeetToMeters(float.Parse(airportFeature.DataRow["ELEVATION"].ToString()));
                        int direction = getDirection(runwayFeature, airportFeature.DataRow["IDENT"].ToString());

                        int length = (int)Math.Round(Utilities.FeetToMeters(float.Parse(runwayFeature.DataRow["LENGTH"].ToString())));
                        int width = (int)Math.Round(Utilities.FeetToMeters(float.Parse(runwayFeature.DataRow["WIDTH"].ToString())));

                        bool asphalt = isAsphalt((string)runwayFeature.DataRow["COMP_CODE"]);

                        PointF[] corners = null;
                        if (coordinateWithinExtent(runwayFeature.Coordinates.FirstOrDefault()))
                        {
                            corners = new PointF[4];
                            for (int i = 0; i < corners.Length; i++)
                            {
                                corners[i] = new PointF((float)runwayFeature.Coordinates[i].X, (float)runwayFeature.Coordinates[i].Y);
                            }
                        }

                        airports.Add(new Airport(name, latitude, longitude, altitude, direction, length, width, asphalt, corners));
                    }
                }
            }

            return airports;
        }

        private bool coordinateWithinExtent(Coordinate coordinate)
        {
            Envelope envelope = new Envelope(SettingsManager.Instance.LatLongTopLeft.X, SettingsManager.Instance.LatLongBottomRight.X, 
                SettingsManager.Instance.LatLongTopLeft.Y, SettingsManager.Instance.LatLongBottomRight.Y);
            return envelope.Contains(coordinate);
        }

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

        private int getDirection(IFeature runwayFeature, string airportId)
        {
            int direction = int.MinValue;
            string designator = runwayFeature.DataRow["DESIGNATOR"].ToString();

            // There is a lot of bad runway data in the shapefile, so first we'll try to get the direction from the NASR database.
            using (SQLiteConnection connection = new SQLiteConnection(string.Format(@"Data Source=""{0}""", Path.Combine(SettingsManager.Instance.InputAirportDir, "nasr.sqlite"))))
            {
                connection.Open();
                string selectAirports = string.Format("SELECT * FROM APT_APT WHERE location_identifier = '{0}'", airportId);
                using (SQLiteCommand airportCommand = new SQLiteCommand(selectAirports, connection))
                {
                    using (SQLiteDataReader airportReader = airportCommand.ExecuteReader())
                    {
                        while (airportReader.Read())
                        {
                            string landing_facility_site_number = airportReader["landing_facility_site_number"].ToString();
                            string selectRunways = string.Format("SELECT * FROM APT_RWY WHERE landing_facility_site_number = '{0}' AND (base_end_identifier = '{1}' OR reciprocal_end_identifier = '{1}')", landing_facility_site_number, designator);
                            using (SQLiteCommand runwayCommand = new SQLiteCommand(selectRunways, connection))
                            {
                                using (SQLiteDataReader runwayReader = runwayCommand.ExecuteReader())
                                {
                                    runwayReader.Read();
                                    string alignment = runwayReader["base_runway_end_true_alignment"].ToString();
                                    if (alignment != string.Empty)
                                    {
                                        int.TryParse(alignment, out direction);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Check to see if we managed to get the direction from the NASR database.
            if(direction == int.MinValue)
            {
                // If we can get valid lat/long from the corners, go ahead and get the direction as well.
                if(coordinateWithinExtent(getCenterLatLongFromCorners(runwayFeature.Coordinates)))
                {
                    direction = getDirectionFromCorners(runwayFeature.Coordinates);
                }
                // As a last resort, extract the direction from the designator.
                else
                {
                    switch(designator)
                    {
                        // Account for cardinal directions
                        case "N":
                            {
                                direction = 360;
                            }
                            break;
                        case "NE":
                            {
                                direction = 45;
                            }
                            break;
                        case "E":
                            {
                                direction = 90;
                            }
                            break;
                        case "SE":
                            {
                                direction = 135;
                            }
                            break;
                        case "S":
                            {
                                direction = 180;
                            }
                            break;
                        case "SW":
                            {
                                direction = 225;
                            }
                            break;
                        case "W":
                            {
                                direction = 270;
                            }
                            break;
                        case "NW":
                            {
                                direction = 315;
                            }
                            break;
                        // Calculate heading from runway number.
                        default:
                            {
                                // Pull any letters out
                                designator = Regex.Replace(designator, "[^0-9.]", "");
                                if(int.TryParse(designator, out direction))
                                {
                                    direction *= 10;
                                }
                            }
                            break;
                    }
                }
            }

            // For some reason, incrementing the direction by 2 degrees gets better results in most cases.
            return direction += 2;
        }

        // TODO: This mostly works, but I think I need to account for magnetic variation.
        private int getDirectionFromCorners(IList<Coordinate> corners)
        {
            int direction;

            direction = (int)Math.Round(Math.Atan2(corners[0].X - corners[3].X, corners[0].Y - corners[3].Y) * (180 / Math.PI));

            if (direction < 0)
            {
                direction = direction + 360;
            }

            return direction;
        }

        private Coordinate getCenterLatLongFromCorners(IList<Coordinate> corners)
        {
            double x = 0;
            double y = 0;
            double z = 0;

            for(int i = 0; i < corners.Count - 1; i++)
            {
                // Convert to radians.
                double latitude = corners[i].Y * Math.PI / 180;
                double longitude = corners[i].X * Math.PI / 180;

                // Get x, y and z component totals
                x += Math.Cos(latitude) * Math.Cos(longitude);
                y += Math.Cos(latitude) * Math.Sin(longitude);
                z += Math.Sin(latitude);
            }

            int total = corners.Count - 1;

            // Average x, y and z components.
            x = x / total;
            y = y / total;
            z = z / total;

            // Calculate latitude and longitude in radians.
            double centralLongitude = Math.Atan2(y, x);
            double centralSquareRoot = Math.Sqrt(x * x + y * y);
            double centralLatitude = Math.Atan2(z, centralSquareRoot);

            // Convert back to degrees
            centralLatitude *= (180.0 / Math.PI);
            centralLongitude *= (180.0 / Math.PI);

            return new Coordinate(centralLongitude, centralLatitude);
        }
    }
}
