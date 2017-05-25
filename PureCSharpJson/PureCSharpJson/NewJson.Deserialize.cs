using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PureCSharpJson.PureCSharpJson {
	public static partial class NewJson{
		private static object ReadArray(JSONArray arrayNode, Type type){
			var newInstance = (IList)Activator.CreateInstance(type, arrayNode.Count);
			var indiceType = type.GetElementType() ?? type.GetGenericArguments().FirstOrDefault();
			if (newInstance is Array){
				var i = 0;
				foreach (JSONNode item in arrayNode)
					newInstance[i++] = HandleNode(item, indiceType);
			}
			else
				foreach (JSONNode item in arrayNode)
					newInstance.Add(HandleNode(item, indiceType));
			return newInstance;
		}

		private static object ReadClass(JSONClass classNode, Type type){
			var newInstance = Activator.CreateInstance(type);
			var properties = type.GetProperties().ToDictionary(p=>p.Name.ToLower());
			foreach (KeyValuePair<string, JSONNode> pair in classNode){
				var key = pair.Key.ToLower();
				if(properties.ContainsKey(key) == false) continue;
				var prop = properties[key];

				if (prop.GetCustomAttributes(typeof(ScriptIgnoreAttribute), true).Any())
					continue;

				if (prop.PropertyType == typeof (string))
					prop.SetValue(newInstance, pair.Value.Value, null);

				else if (prop.PropertyType.IsClass)
					prop.SetValue(newInstance, HandleNode(pair.Value, prop.PropertyType), null);

				else{
					var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(pair.Value.Value);
					prop.SetValue(newInstance, value, null);
				}
			}

			return newInstance;
		}

		private static object HandleNode(JSONNode node, Type type) {
			var arrayNode = node as JSONArray;
			if (arrayNode != null)
				return ReadArray(arrayNode,  type);

			var classNode = node as JSONClass;
			if (classNode != null)
				return ReadClass(classNode, type);

			var nullNode = node as JSONNull;
			if (nullNode != null)
				return null;

			var dataNode = node as JSONData;
			if (dataNode != null)
				return TypeDescriptor.GetConverter(type).ConvertFromString(dataNode.Value);

			throw new NotImplementedException();
		}

		public static object Deserialize(string json, Type type){
			var parsed = JSONNode.Parse(json);
			var obj = HandleNode(parsed, type);
			return obj;
		}

		public static T Deserialize<T>(string json) {
			return (T)Deserialize(json, typeof(T));
		}
	}
}
