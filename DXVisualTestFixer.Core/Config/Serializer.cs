using System;
using System.Configuration;
using System.IO;
using DXVisualTestFixer.Common;
using Polenter.Serialization;
using Polenter.Serialization.Advanced;
using Polenter.Serialization.Advanced.Serializing;
using TypeNameConverter = Polenter.Serialization.Advanced.TypeNameConverter;

namespace DXVisualTestFixer.Core.Configuration {
	public static class Serializer {
		public static void Serialize<T>(string path, T value) {
			var settings = new SharpSerializerXmlSettings();
			settings.IncludeAssemblyVersionInTypeName = false;
			settings.IncludePublicKeyTokenInTypeName = false;
			var serializer = new SharpSerializer(settings);
			serializer.Serialize(value, path);
		}

		public static void Serialize<T>(Stream stream, T value) {
			var settings = new SharpSerializerXmlSettings();
			settings.IncludeAssemblyVersionInTypeName = false;
			settings.IncludePublicKeyTokenInTypeName = false;
			var serializer = new SharpSerializer(settings);
			serializer.Serialize(value, stream);
		}

		public static T Deserialize<T>(string path) {
			var settings = new SharpSerializerXmlSettings();
			settings.IncludeAssemblyVersionInTypeName = false;
			settings.IncludePublicKeyTokenInTypeName = false;
			settings.AdvancedSettings.TypeNameConverter = new LegacyTypeNameConverter();
			var serializer = new SharpSerializer(settings);
			return (T) serializer.Deserialize(path);
		}

		class LegacyTypeNameConverter : ITypeNameConverter {
			readonly TypeNameConverter innerConverter = new TypeNameConverter();

			public Type ConvertToType(string typeName) {
				if(typeName != null && typeName.Contains("Config"))
					return typeof(Config);
				if(typeName != null && typeName.Contains("Team"))
					return typeof(Team);
				return innerConverter.ConvertToType(typeName);
			}

			public string ConvertToTypeName(Type type) {
				return innerConverter.ConvertToTypeName(type);
			}
		}
	}
}