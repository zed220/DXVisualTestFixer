using Polenter.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Configuration {
    public static class Serializer {
        public static void Serialize<T>(string path, T value) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(value, path);
        }
        public static void Serialize<T>(Stream stream, T value) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            serializer.Serialize(value, stream);
        }
        public static T Deserialize<T>(string path) {
            SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
            settings.IncludeAssemblyVersionInTypeName = false;
            settings.IncludePublicKeyTokenInTypeName = false;
            SharpSerializer serializer = new SharpSerializer(settings);
            return (T)serializer.Deserialize(path);
        }
        public static T Deserialize<T>(Stream stream) {
            try {
                SharpSerializerXmlSettings settings = new SharpSerializerXmlSettings();
                settings.IncludeAssemblyVersionInTypeName = false;
                settings.IncludePublicKeyTokenInTypeName = false;
                SharpSerializer serializer = new SharpSerializer(settings);
                return (T)serializer.Deserialize(stream);
            }
            catch(Exception ex) {
                return default(T);
            }
        }
    }
}
