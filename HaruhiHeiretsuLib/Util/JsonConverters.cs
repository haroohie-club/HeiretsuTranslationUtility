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
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<SgeBoneAttachedVertex, float> value, JsonSerializerOptions options)
        {
            Dictionary<string, float> convertedKvp = value.ToDictionary(kv => $"{kv.Key.Mesh},{kv.Key.VertexIndex}", kv => kv.Value);
            writer.WriteStartObject();
            foreach (string key in convertedKvp.Keys)
            {
                writer.WriteNumber(key, convertedKvp[key]);
            }
            writer.WriteEndObject();
        }
    }
}
