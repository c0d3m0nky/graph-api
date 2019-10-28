using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AADB2C.GraphApi.PutOnNuget
{
    public class FileSystemInfoJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(FileSystemInfo).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string s)
            {
                return File.GetAttributes(s).HasFlag(FileAttributes.Directory) ? (FileSystemInfo) new DirectoryInfo(s) : new FileInfo(s);
            }

            throw new ArgumentOutOfRangeException(nameof(reader));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is FileSystemInfo fsi)) throw new ArgumentOutOfRangeException(nameof(value));

            writer.WriteValue(fsi.FullName);
        }
    }
}