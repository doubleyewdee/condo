namespace condo
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media;
    using ConsoleBuffer;
    using Newtonsoft.Json;

    sealed partial class Configuration
    {
        sealed class PaletteConverter : JsonConverter<List<Character.ColorInfo>>
        {
            public override List<Character.ColorInfo> ReadJson(JsonReader reader, Type objectType, List<Character.ColorInfo> existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartArray)
                {
                    throw new JsonException("Expected start of array");
                }

                var palette = new List<Character.ColorInfo>(16);
                reader.Read();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    var color = (Color)ColorConverter.ConvertFromString((string)reader.Value);
                    palette.Add(new Character.ColorInfo { R = color.R, G = color.G, B = color.B });
                    if (!reader.Read())
                        throw new JsonException("Unexpected end of array");
                }

                return palette;
            }

            public override void WriteJson(JsonWriter writer, List<Character.ColorInfo> value, JsonSerializer serializer)
            {
                var converter = new ColorConverter();
                writer.WriteStartArray();
                for (var i = 0; i < value.Count; ++i)
                {
                    var color = new Color { R = value[i].R, G = value[i].G, B = value[i].B };
                    writer.WriteValue(converter.ConvertToString(color));
                }
                writer.WriteEndArray();
            }
        }

        [JsonProperty(Order = -1), JsonConverter(typeof(PaletteConverter))]
        public List<Character.ColorInfo> Palette { get; set; } = new List<Character.ColorInfo>
        {
            new Character.ColorInfo { R = 0x1d, G = 0x1f, B = 0x21 },
            new Character.ColorInfo { R = 0xa5, G = 0x42, B = 0x42 },
            new Character.ColorInfo { R = 0x8c, G = 0x94, B = 0x40 },
            new Character.ColorInfo { R = 0xde, G = 0x93, B = 0x5f },
            new Character.ColorInfo { R = 0x5f, G = 0x81, B = 0x9d },
            new Character.ColorInfo { R = 0x85, G = 0x67, B = 0x8f },
            new Character.ColorInfo { R = 0x5e, G = 0x8d, B = 0x87 },
            new Character.ColorInfo { R = 0x70, G = 0x78, B = 0x80 },
            new Character.ColorInfo { R = 0x37, G = 0x3b, B = 0x41 },
            new Character.ColorInfo { R = 0xcc, G = 0x66, B = 0x66 },
            new Character.ColorInfo { R = 0xb5, G = 0xbd, B = 0x68 },
            new Character.ColorInfo { R = 0xf0, G = 0xc6, B = 0x74 },
            new Character.ColorInfo { R = 0x81, G = 0xa2, B = 0xbe },
            new Character.ColorInfo { R = 0xb2, G = 0x94, B = 0xbb },
            new Character.ColorInfo { R = 0x8a, G = 0xbe, B = 0xb7 },
            new Character.ColorInfo { R = 0xc5, G = 0xc8, B = 0xc6 },
        };
    }
}
