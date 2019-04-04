using System;
using System.Collections.Generic;
using System.Text;

namespace LandscapeBuilderLib
{
    partial class Airport
    {
        public WorldObjectList GrassObjects { get; private set; } = new WorldObjectList();
        public WorldObjectList OtherObjects { get; private set; } = new WorldObjectList();

        public void GenerateObjects()
        {
            addRunway();
            addWindsock(Width / 2f + 5);

            GrassObjects.ToObjMtl(string.Format("{0}G", Name));
            OtherObjects.ToObjMtl(string.Format("{0}O", Name));
        }

        private void addRunway()
        {
            WorldObject runway = new WorldObject();
            runway.Name = Asphalt ? "Asphalt" : "Grass";

            float x = Length / 2.0f;
            float y = 0.001f;
            float z = Width / 2.0f;

            runway.Vertices.Add(new Vertex(-x, y, z));
            runway.Vertices.Add(new Vertex(x, y, z));
            runway.Vertices.Add(new Vertex(x, y, -z));
            runway.Vertices.Add(new Vertex(-x, y, -z));

            runway.VertexNormals.Add(new Vertex(0, 1, 0));

            // This determines the size of the grass/asphalt texture.
            // TODO: Need to make this customizable.
            runway.TextureCoordinates.Add(new Vertex(0, 0, 0));
            runway.TextureCoordinates.Add(new Vertex(0.8f, 0, 0));
            runway.TextureCoordinates.Add(new Vertex(0.8f, 0.12f, 0));
            runway.TextureCoordinates.Add(new Vertex(0f, 0.12f, 0));

            Face face = new Face();
            face.Add(new FaceDatum(1, 1, 1));
            face.Add(new FaceDatum(2, 2, 1));
            face.Add(new FaceDatum(3, 3, 1));
            runway.Faces.Add(face);

            Face face2 = new Face();
            face2.Add(new FaceDatum(3, 3, 1));
            face2.Add(new FaceDatum(4, 4, 1));
            face2.Add(new FaceDatum(1, 1, 1));
            runway.Faces.Add(face2);

            Material runwayMaterial = new Material()
            {
                Name = "01_Default",
                SpecularExponent = 10f,
                OpticalDensity = 1.5f,
                Dissolved = 1,
                TransmissionFilter = new MtlColor(1f, 1f, 1f),
                IlluminationModel = 2,
                AmbientColor = new MtlColor(1f, 1f, 1f),
                DiffuseColor = new MtlColor(1f, 1f, 1f),
                SpecularColor = new MtlColor(0, 0, 0)
            };

            GrassObjects.Add(runwayMaterial, runway);
        }

        // These functions load the data for the windsock and windsock pole from the pre-generated .obj and .mtl files.
        #region Windsock
        private void addWindsock(float offsetX = 0, float offsetZ = 0)
        {
            Material windsockMaterial = getWindsockMaterial();

            WorldObject windsockPole = new WorldObject()
            {
                Name = "Pole",
                Vertices = getWindsockPoleVertices(offsetX, offsetZ),
                VertexNormals = getWindsockPoleNormals(),
                TextureCoordinates = getWindsockPoleTextureCoordinates(),
                Faces = getWindsockPoleFaces()
            };


            WorldObject windsock = new WorldObject()
            {
                Name = "Windsack1",
                Vertices = getWindsockVertices(offsetX, offsetZ),
                VertexNormals = getWindsockNormals(),
                Faces = getWindsockFaces()
            };

            OtherObjects.Add(windsockMaterial, windsockPole);
            OtherObjects.Add(windsockMaterial, windsock);
        }

        private List<Vertex> getWindsockPoleVertices(float offsetX, float offsetZ)
        {
            List<Vertex> vertices = loadWindsockObjVertices(0x42, 0x4D2);
            foreach (Vertex vertex in vertices)
            {
                vertex.X += offsetX;
                vertex.Z += offsetZ;
            }

            return vertices;
        }

        private List<Vertex> getWindsockVertices(float offsetX, float offsetZ)
        {
            List<Vertex> vertices = loadWindsockObjVertices(0x1767, 0x49);
            foreach (Vertex vertex in vertices)
            {
                vertex.X += offsetX;
                vertex.Z += offsetZ;
            }

            return vertices;
        }

        private List<Vertex> getWindsockPoleNormals()
        {
            return loadWindsockObjVertices(0x518, 0x244);
        }

        private List<Vertex> getWindsockNormals()
        {
            return loadWindsockObjVertices(0x17B4, 0x18);
        }

        private List<Vertex> getWindsockPoleTextureCoordinates()
        {
            return loadWindsockObjVertices(0x760, 0x57C);
        }

        private List<Face> getWindsockPoleFaces()
        {
            return loadWindsockObjFaces(0xD00, 0xA63);
        }

        private List<Face> getWindsockFaces()
        {
            return loadWindsockObjFaces(0x17F5, 0x16);
        }

        private List<Vertex> loadWindsockObjVertices(int index, int count)
        {
            string[] vertexStrings = Encoding.ASCII.GetString(Properties.Resources.WindsockOBJ, index, count).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            List<Vertex> vertices = new List<Vertex>();
            foreach (string vertex in vertexStrings)
            {
                string[] coordinates = vertex.Split(' ');
                float x = float.Parse(coordinates[1]);
                float y = float.Parse(coordinates[2]);
                float z = float.Parse(coordinates[3]);

                vertices.Add(new Vertex(x, y, z));
            }

            return vertices;
        }

        private List<Face> loadWindsockObjFaces(int index, int count)
        {
            string[] faceStrings = Encoding.ASCII.GetString(Properties.Resources.WindsockOBJ, index, count).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            List<Face> faces = new List<Face>();
            foreach(string faceString in faceStrings)
            {
                Face face = new Face();
                string[] faceData = faceString.Split(' ');
                if (faceData[0] != "s")
                {
                    foreach (string faceDatum in faceData)
                    {
                        if (faceDatum != "f")
                        {
                            string[] indices = faceDatum.Split('/');
                            // Vertex will always be present.
                            int vertexIndex = int.Parse(indices[0]);

                            // Texture and normal may not be.
                            string texture = indices[1];
                            string normal = indices[2];

                            int textureIndex;
                            if (texture == string.Empty)
                            {
                                textureIndex = 0;
                            }
                            else
                            {
                                textureIndex = int.Parse(texture);
                            }

                            int normalIndex;
                            if (normal == string.Empty)
                            {
                                normalIndex = 0;
                            }
                            else
                            {
                                normalIndex = int.Parse(normal);
                            }
                            face.Add(new FaceDatum(vertexIndex, textureIndex, normalIndex));
                        }
                    }
                }

                faces.Add(face);
            }
            return faces;
        }

        // Creates a Material object from the pre-generated .mtl file for the windsock.
        private Material getWindsockMaterial()
        {
            Material material = new Material();
            string[] windsockMtl = Encoding.ASCII.GetString(Properties.Resources.WindsockMTL).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach(string mtlEntry in windsockMtl)
            {
                string[] mtl = mtlEntry.TrimStart(' ').Split(' ');
                switch(mtl[0])
                {
                    case "newmtl":
                        {
                            material.Name = mtl[1];
                        }
                        break;
                    case "Ns":
                        {
                            material.SpecularExponent = float.Parse(mtl[1]);
                        }
                        break;
                    case "Ni":
                        {
                            material.OpticalDensity = float.Parse(mtl[1]);
                        }
                        break;
                    case "d":
                        {
                            material.Dissolved = float.Parse(mtl[1]);
                        }
                        break;
                    case "Tf":
                        {
                            float red = float.Parse(mtl[1]);
                            float green = float.Parse(mtl[2]);
                            float blue = float.Parse(mtl[3]);
                            material.TransmissionFilter = new MtlColor(red, green, blue);
                        }
                        break;
                    case "illum":
                        {
                            material.IlluminationModel = int.Parse(mtl[1]);
                        }
                        break;
                    case "Ka":
                        {
                            float red = float.Parse(mtl[1]);
                            float green = float.Parse(mtl[2]);
                            float blue = float.Parse(mtl[3]);
                            material.AmbientColor = new MtlColor(red, green, blue);
                        }
                        break;
                    case "Kd":
                        {
                            float red = float.Parse(mtl[1]);
                            float green = float.Parse(mtl[2]);
                            float blue = float.Parse(mtl[3]);
                            material.DiffuseColor = new MtlColor(red, green, blue);
                        }
                        break;
                    case "Ks":
                        {
                            float red = float.Parse(mtl[1]);
                            float green = float.Parse(mtl[2]);
                            float blue = float.Parse(mtl[3]);
                            material.SpecularColor = new MtlColor(red, green, blue);
                        }
                        break;
                }
            }

            return material;
        }
        #endregion
    }
}
