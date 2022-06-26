using grendgine_collada;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HaruhiHeiretsuLib.Graphics.Renderer
{
    public class SgeColladaRenderer
    {
        public Grendgine_Collada DaeObject { get; private set; } = new Grendgine_Collada();  // This is the serializable class.
        private readonly XmlSerializer _mySerializer = new(typeof(Grendgine_Collada));

        public SgeModel Sge { get; set; }

        public MemoryStream Render()
        {
            GenerateDaeObject();

            Console.WriteLine("*** Starting WriteCOLLADA() ***");

            MemoryStream dae = new();

            using StreamWriter writer = new(dae);
            _mySerializer.Serialize(writer, DaeObject);

            Console.WriteLine("End of Write Collada.  Export complete.");
            return dae;
        }

        public void GenerateDaeObject()
        {
            DaeObject.Collada_Version = "1.4.1";
            AddContributors();
            AddLibraryImages();
            AddLibraryMaterials();
            AddLibraryEffects();
            AddLibraryGeometries();
        }

        private void AddContributors()
        {
            // Writes the Asset element in a Collada XML doc
            DateTime fileCreated = DateTime.UtcNow;
            DateTime fileModified = DateTime.UtcNow;
            Grendgine_Collada_Asset asset = new()
            {
                Revision = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            };
            Grendgine_Collada_Asset_Contributor[] contributors = new Grendgine_Collada_Asset_Contributor[2];
            contributors[0] = new Grendgine_Collada_Asset_Contributor
            {
                Authoring_Tool = "Heiretsu SGE Library",
                Source_Data = Sge.Name
            };

            asset.Created = fileCreated;
            asset.Modified = fileModified;
            asset.Up_Axis = "Z_UP";
            asset.Unit = new Grendgine_Collada_Asset_Unit()
            {
                Meter = 1.0,
                Name = "meter"
            };
            asset.Title = Sge.Name;
            DaeObject.Asset = asset;
            DaeObject.Asset.Contributor = contributors;
            DaeObject.Asset.Unit = new() { Meter = 0.0245, Name = "centimeter" };
        }

        private void AddLibraryImages()
        {
            DaeObject.Library_Images = new();
            List<Grendgine_Collada_Image> imageList = new();

            foreach (SgeMaterial material in Sge.SgeMaterials)
            {
                string texFileName = Path.Combine(Path.GetTempPath(), $"{material.Name}.png");
                File.WriteAllBytes(texFileName, material.Texture.GetImage().Bytes);
                imageList.Add(new()
                {
                    ID = $"{material.Name}-image",
                    Name = material.Name,
                    Init_From = new Grendgine_Collada_Init_From() { Ref = texFileName },
                });
            }
            DaeObject.Library_Images.Image = imageList.ToArray();
        }

        private void AddLibraryMaterials()
        {
            DaeObject.Library_Materials = new();
            List<Grendgine_Collada_Material> materialList = new();

            foreach (SgeMaterial material in Sge.SgeMaterials)
            {
                materialList.Add(new()
                {
                    ID = material.Name,
                    Name = material.Name,
                    Instance_Effect = new()
                    {
                        URL = $"#{material.Name}-fx",
                    }
                });
            }

            DaeObject.Library_Materials.Material = materialList.ToArray();
        }

        private void AddLibraryEffects()
        {
            DaeObject.Library_Effects = new();
            List<Grendgine_Collada_Effect> effects = new();

            foreach (SgeMaterial material in Sge.SgeMaterials)
            {
                List<Grendgine_Collada_Profile_COMMON> profiles = new();
                XmlDocument extras = new();
                XmlElement wrapU = extras.CreateElement("wrapU");
                wrapU.Attributes.Append(extras.CreateAttribute("sid"));
                wrapU.Attributes[0].Value = "wrapU0";
                wrapU.InnerText = "TRUE";
                XmlElement wrapV = extras.CreateElement("wrapV");
                wrapV.Attributes.Append(extras.CreateAttribute("sid"));
                wrapV.Attributes[0].Value = "wrapV0";
                wrapV.InnerText = "TRUE";
                XmlElement blendMode = extras.CreateElement("blend_mode");
                blendMode.InnerText = "MULTIPLY";

                profiles.Add(new()
                {
                    Technique = new()
                    {
                        sID = "standard",
                        Phong = new()
                        {
                            Emission = new()
                            {
                                Color = new() { sID = "specular", Value_As_String = "0.000000  0.000000 0.000000 1.000000" }
                            },
                            Ambient = new()
                            {
                                Color = new() { sID = "ambient", Value_As_String = "0.000000  0.000000 0.000000 1.000000" }
                            },
                            Diffuse = new()
                            {
                                Texture = new()
                                {
                                    Texture = $"{material.Name}-image",
                                    TexCoord = "CHANNEL0",
                                    Extra = new Grendgine_Collada_Extra[1]
                                    {
                                        new()
                                        {
                                            Technique = new Grendgine_Collada_Technique[1]
                                            {
                                                new()
                                                {
                                                    profile = "MAYA",
                                                    Data = new XmlElement[3]
                                                    {
                                                        wrapU,
                                                        wrapV,
                                                        blendMode,
                                                    },
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            Specular = new()
                            {
                                Color = new() { sID = "specular", Value_As_String = "0.000000  0.000000 0.000000 1.000000" },
                            },
                            Shininess = new()
                            {
                                Float = new() { sID = "shininess", Value = 2.0f },
                            },
                            Reflective = new()
                            {
                                Color = new() { sID = "reflective", Value_As_String = "0.000000  0.000000 0.000000 1.000000" },
                            },
                            Reflectivity = new()
                            {
                                Float = new() { sID = "reflectivity", Value = 1.0f },
                            },
                            Transparent = new()
                            {
                                Opaque = Grendgine_Collada_FX_Opaque_Channel.RGB_ZERO,
                                Color = new() { sID = "transparent", Value_As_String = "1.000000  1.000000 1.000000 1.000000" },
                            },
                            Transparency = new()
                            {
                                Float = new() { sID = "transparency", Value = 0.0f },
                            }
                        },
                    }
                });

                effects.Add(new()
                {
                    ID = $"{material.Name}-fx",
                    Name = material.Name,
                    Profile_COMMON = profiles.ToArray(),
                });
            }

            DaeObject.Library_Effects.Effect = effects.ToArray();
        }

        private void AddLibraryGeometries()
        {
            DaeObject.Library_Geometries = new();
            List<Grendgine_Collada_Geometry> geometries = new();
            Grendgine_Collada_Mesh mesh = new();
            List<Grendgine_Collada_Source> sources = new();

            Grendgine_Collada_Source positionSource = new()
            {
                ID = $"{Sge.Name}-POSITION",
                Float_Array = new()
                {
                    ID = $"{Sge.Name}-POSITION-array",
                    Count = Sge.SgeVertices.Count * 3,
                    Value_As_String = string.Join('\n', Sge.SgeVertices.Select(v => $"{Safe(v.Position.X)} {Safe(v.Position.Y)} {Safe(v.Position.Z)}")),
                },
                Technique_Common = new()
                {
                    Accessor = new()
                    {
                        Source = $"#{Sge.Name}-POSITION-array",
                        Count = (uint)Sge.SgeVertices.Count,
                        Stride = 3,
                        Param = new Grendgine_Collada_Param[3]
                        {
                            new() { Name = "X", Type = "float" },
                            new() { Name = "Y", Type = "float" },
                            new() { Name = "Z", Type = "float" },
                        },
                    }
                }
            };
            sources.Add(positionSource);

            Grendgine_Collada_Source normalsSource = new()
            {
                ID = $"{Sge.Name}-Normal0",
                Float_Array = new()
                {
                    ID = $"{Sge.Name}-Normal0-array",
                    Count = Sge.SgeFaces.Count * 9,
                    Value_As_String = string.Join('\n', Sge.SgeFaces.Select(f => string.Join('\n', f.Polygon.Select(p => $"{Safe(Sge.SgeVertices[p].Normal.X)} {Safe(Sge.SgeVertices[p].Normal.Y)} {Safe(Sge.SgeVertices[p].Normal.Z)}")))),
                },
                Technique_Common = new()
                {
                    Accessor = new()
                    {
                        Source = $"#{Sge.Name}-Normal0-array",
                        Count = (uint)Sge.SgeFaces.Count * 3,
                        Stride = 3,
                        Param = new Grendgine_Collada_Param[3]
                        {
                            new() { Name = "X", Type = "float" },
                            new() { Name = "Y", Type = "float" },
                            new() { Name = "Z", Type = "float" },
                        },
                    }
                }
            };
            sources.Add(normalsSource);

            Grendgine_Collada_Source uvSource = new()
            {
                ID = $"{Sge.Name}-UV0",
                Float_Array = new()
                {
                    ID = $"{Sge.Name}-UV0-array",
                    Count = Sge.SgeVertices.Count * 2,
                    Value_As_String = string.Join('\n', Sge.SgeVertices.Select(v => $"{Safe(v.UVCoords.X)} {Safe(v.UVCoords.Y)}")),
                },
                Technique_Common = new()
                {
                    Accessor = new()
                    {
                        Source = $"#{Sge.Name}-UV0-array",
                        Count = (uint)Sge.SgeVertices.Count,
                        Stride = 2,
                        Param = new Grendgine_Collada_Param[2]
                        {
                            new() { Name = "S", Type = "float" },
                            new() { Name = "T", Type = "float" },
                        },
                    }
                }
            };
            sources.Add(uvSource);

            mesh.Source = sources.ToArray();

            mesh.Vertices = new()
            {
                ID = $"{Sge.Name}-VERTEX",
                Input = new Grendgine_Collada_Input_Unshared[1]
                {
                    new()
                    {
                        Semantic = Grendgine_Collada_Input_Semantic.POSITION,
                        source = $"#{Sge.Name}-POSITION",
                    },
                }
            };

            List<Grendgine_Collada_Triangles> triangles = new();
            foreach (SgeMaterial material in Sge.SgeMaterials)
            {
                IEnumerable<SgeFace> faces = Sge.SgeFaces.Where(f => f.Material.Name == material.Name);

                triangles.Add(new()
                {
                    Input = new Grendgine_Collada_Input_Shared[3]
                    {
                        new()
                        {
                            Semantic = Grendgine_Collada_Input_Semantic.VERTEX,
                            Offset = 0,
                            source = $"#{Sge.Name}-VERTEX"
                        },
                        new()
                        {
                            Semantic = Grendgine_Collada_Input_Semantic.NORMAL,
                            Offset = 1,
                            source = $"#{Sge.Name}-Normal0",
                        },
                        new()
                        {
                            Semantic = Grendgine_Collada_Input_Semantic.TEXCOORD,
                            Offset = 2,
                            Set = 0,
                            source = $"#{Sge.Name}-UV0",
                        },
                    },
                    P = new()
                    {
                        Value_As_String = string.Join(' ', faces.Select(f => string.Join(' ', f.Polygon)))
                    }
                });
            }

            mesh.Triangles = triangles.ToArray();

            Grendgine_Collada_Geometry geometry = new() { ID = $"{Sge.Name}-lib", Name = $"{Sge.Name}Mesh", Mesh = mesh };
            geometries.Add(geometry);
            DaeObject.Library_Geometries.Geometry = geometries.ToArray();
        }

        private void AddLibraryControllers()
        {

        }

        private static float Safe(float value)
        {
            if (value == float.NegativeInfinity)
                return float.MinValue;

            if (value == float.PositiveInfinity)
                return float.MaxValue;

            if (float.IsNaN(value))
                return 0;

            return value;
        }
    }
}
