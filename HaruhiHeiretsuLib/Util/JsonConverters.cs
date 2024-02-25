using HaruhiHeiretsuLib.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaruhiHeiretsuLib.Util
{
    public class SgeBoneAttchedVertexConverter : JsonConverter<Dictionary<SgeBoneAttachedVertex, float>>
    {
        public override Dictionary<SgeBoneAttachedVertex, float> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Dictionary<SgeBoneAttachedVertex, float> values = [];

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read();
            }
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                string boneData = reader.GetString();
                reader.Read();
                if (string.IsNullOrEmpty(boneData))
                {
                    throw new InvalidOperationException("Bone data in SgeBoneAttachedVertex dictionary was null or empty!");
                }
                string[] boneDataSplit = boneData.Split(',');
                SgeBoneAttachedVertex boneAttachedVertex = new(int.Parse(boneDataSplit[0]), int.Parse(boneDataSplit[1]), int.Parse(boneDataSplit[2]));
                float weight = reader.GetSingle();
                reader.Read();
                values.Add(boneAttachedVertex, weight);
            }

            return values;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<SgeBoneAttachedVertex, float> value, JsonSerializerOptions options)
        {
            Dictionary<string, float> convertedKvp = value.ToDictionary(kv => $"{kv.Key.SubmeshGroup},{kv.Key.Submesh},{kv.Key.VertexIndex}", kv => kv.Value);
            writer.WriteStartObject();
            foreach (string key in convertedKvp.Keys)
            {
                writer.WriteNumber(key, convertedKvp[key]);
            }
            writer.WriteEndObject();
        }
    }
}
