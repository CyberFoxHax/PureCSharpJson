using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PureCSharpJson.PureCSharpJson {
	public static partial class PureCSharpJson{

		private static readonly Dictionary<Type, Func<object, JSONData>> ConversionMap = new Dictionary<Type, Func<object, JSONData>>{
			{typeof(string)	, p=>new JSONData((string)p)},
			{typeof(float)	, p=>new JSONData((float)p)},
			{typeof(double)	, p=>new JSONData((double)p)},
			{typeof(bool)	, p=>new JSONData((bool)p)},
			{typeof(int)	, p=>new JSONData((int)p)}
		};

		private static JSONData GetJsonData(object obj){
			if(obj == null)
				return null;
			var objType = obj.GetType();

			if (objType.IsValueType && Equals(obj, Activator.CreateInstance(objType)))
				return null;
			if (ConversionMap.ContainsKey(objType))
				return ConversionMap[objType](obj);

			return null;
		}

		private static JSONArray GetJsonArray(IEnumerable ienumerable){
			var array = ienumerable.Cast<object>().ToArray();
			var firstItem = array.FirstOrDefault(p=>p!=null);
			if (firstItem == null)
				return null;
			var jsonArr = new JSONArray();
			foreach (var item in array)
				jsonArr.Add(GetJsonNode(item));
			return jsonArr;
		}

		private static JSONClass GetJsonClass(object obj){
			var jsonClass = new JSONClass();
			var props = obj.GetType().GetProperties();
			foreach (var propertyInfo in props) {
				if (propertyInfo.GetCustomAttributes(typeof(ScriptIgnoreAttribute), true).Any())
					continue;

				var propValue = propertyInfo.GetValue(obj, null);
				var jsonData = GetJsonNode(propValue);

				if (jsonData != null)
					jsonClass.Add(propertyInfo.Name, jsonData);
			}

			return jsonClass;
		}

		private static JSONNode GetJsonNode(object obj){
			if (obj == null)
				return new JSONNull();

			var objectType = obj.GetType();

			if (objectType == typeof(string))
				return GetJsonData(obj);

			var ienumerable = obj as IEnumerable;
			if (ienumerable != null)
				return GetJsonArray(ienumerable);

			if(objectType.IsEnum)
				return GetJsonData((int)obj);

			if (objectType.IsClass)
				return GetJsonClass(obj);

			return GetJsonData(obj);
		}

		public static string Serialize(object obj){
			if (obj == null)
				return "{}";

			var root = GetJsonNode(obj);
			return root.ToJSON(0);
		}
		
	}
}
