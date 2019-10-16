using System;
using System.IO;
using DXVisualTestFixer.Common;
using Polenter.Serialization;
using Polenter.Serialization.Advanced;
using Polenter.Serialization.Advanced.Serializing;

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

		public static T Deserialize<T>(Stream stream) {
			try {
				var settings = new SharpSerializerXmlSettings();
				settings.IncludeAssemblyVersionInTypeName = false;
				settings.IncludePublicKeyTokenInTypeName = false;
				var serializer = new SharpSerializer(settings);
				return (T) serializer.Deserialize(stream);
			}
			catch(Exception) {
				return default;
			}
		}

		static void ConvertConfigToActualVersion(string path) {
			var configAsText = File.ReadAllText(path);
			var fixedConfig = configAsText.Replace("DXVisualTestFixer.Configuration.Config, DXVisualTestFixer, ", "DXVisualTestFixer.Core.Configuration.Config, DXVisualTestFixer.Core, ");
			File.WriteAllText(path, fixedConfig);
		}
	}

	public class LegacyTypeNameConverter : ITypeNameConverter {
		readonly TypeNameConverter innreConverter = new TypeNameConverter();

		public Type ConvertToType(string typeName) {
			if(typeName != null && typeName.Contains("Config"))
				return typeof(Config);
			if(typeName != null && typeName.Contains("Team"))
				return typeof(Team);
			return innreConverter.ConvertToType(typeName);
		}

		public string ConvertToTypeName(Type type) {
			return innreConverter.ConvertToTypeName(type);
		}
	}
}