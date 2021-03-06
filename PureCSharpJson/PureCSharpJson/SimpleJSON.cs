﻿//#define USE_SharpZipLib
#if !UNITY_WEBPLAYER
#define USE_FileIO
#endif
/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 * 
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 * 
 * If you want to use compression when saving to file / stream / B64 you have to include
 * SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ ) in your project and
 * define "USE_SharpZipLib" at the top of the file
 * 
 * Written by Bunny83 
 * 2012-06-09
 * 
 * Modified by oPless, 2014-09-21 to round-trip properly
 *
 * Features / attributes:
 * - provides strongly typed node classes and lists / dictionaries
 * - provides easy access to class members / array items / data values
 * - the parser ignores data types. Each value is a string.
 * - only double quotes (") are used for quoting strings.
 * - values and names are not restricted to quoted strings. They simply add up and are trimmed.
 * - There are only 3 types: arrays(JSONArray), objects(JSONClass) and values(JSONData)
 * - provides "casting" properties to easily convert to / from those types:
 *   int / float / double / bool
 * - provides a common interface for each node so no explicit casting is required.
 * - the parser try to avoid errors, but if malformed JSON is parsed the result is undefined
 * 
 * 
 * 2012-12-17 Update:
 * - Added internal JSONLazyCreator class which simplifies the construction of a JSON tree
 *   Now you can simple reference any item that doesn't exist yet and it will return a JSONLazyCreator
 *   The class determines the required type by it's further use, creates the type and removes itself.
 * - Added binary serialization / deserialization.
 * - Added support for BZip2 zipped binary format. Requires the SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ )
 *   The usage of the SharpZipLib library can be disabled by removing or commenting out the USE_SharpZipLib define at the top
 * - The serializer uses different types when it comes to store the values. Since my data values
 *   are all of type string, the serializer will "try" which format fits best. The order is: int, float, double, bool, string.
 *   It's not the most efficient way but for a moderate amount of data it should work on all platforms.
 * 
 * * * * */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PureCSharpJson.PureCSharpJson {
	public enum JSONBinaryTag {
		Array = 1,
		Class = 2,
		Value = 3,
		IntValue = 4,
		DoubleValue = 5,
		BoolValue = 6,
		FloatValue = 7,
		NullValue = 8,
	}

	public abstract class JSONNode {
		protected bool Equals(JSONNode other){
			return Tag == other.Tag;
		}

		public override int GetHashCode(){
			return (int) Tag;
		}

		#region common interface

		public virtual void Add(string aKey, JSONNode aItem) {
		}

		public virtual JSONNode this[int aIndex] { get { return null; } set { } }

		public virtual JSONNode this[string aKey] { get { return null; } set { } }

		public virtual string Value { get { return ""; } set { } }

		public virtual int Count { get { return 0; } }

		public virtual void Add(JSONNode aItem) {
			Add("", aItem);
		}

		public virtual JSONNode Remove(string aKey) {
			return null;
		}

		public virtual JSONNode Remove(int aIndex) {
			return null;
		}

		public virtual JSONNode Remove(JSONNode aNode) {
			return aNode;
		}

		public virtual IEnumerable<JSONNode> Children {
			get {
				yield break;
			}
		}

		public IEnumerable<JSONNode> DeepChildren {
			get {
				return Children.SelectMany(c => c.DeepChildren);
			}
		}

		public override string ToString() {
			return "JSONNode";
		}

		public virtual string ToString(string aPrefix) {
			return "JSONNode";
		}

		public abstract string ToJSON(int prefix);

		#endregion common interface

		#region typecasting properties

		public virtual JSONBinaryTag Tag { get; set; }

		public virtual int AsInt {
			get {
				int v;
				return int.TryParse(Value, out v) ? v : 0;
			}
			set {
				Value = value + "";
				Tag = JSONBinaryTag.IntValue;
			}
		}

		public virtual float AsFloat {
			get {
				float v;
				return float.TryParse(Value, out v) ? v : 0.0f;
			}
			set {
				Value = value + "";
				Tag = JSONBinaryTag.FloatValue;
			}
		}

		public virtual double AsDouble {
			get {
				double v;
				return double.TryParse(Value, out v) ? v : 0.0;
			}
			set {
				Value = value + "";
				Tag = JSONBinaryTag.DoubleValue;

			}
		}

		public virtual bool AsBool {
			get {
				bool v;
				if (bool.TryParse(Value, out v))
					return v;
				return !string.IsNullOrEmpty(Value);
			}
			set {
				Value = (value) ? "true" : "false";
				Tag = JSONBinaryTag.BoolValue;

			}
		}

		public virtual JSONArray AsArray {
			get {
				return this as JSONArray;
			}
		}

		public virtual JSONClass AsObject {
			get {
				return this as JSONClass;
			}
		}


		#endregion typecasting properties

		#region operators

		public static implicit operator JSONNode(string s) {
			return new JSONData(s);
		}

		public static implicit operator string(JSONNode d) {
			return (d == null) ? null : d.Value;
		}

		public static bool operator ==(JSONNode a, object b) {
			if (b == null && a is JSONLazyCreator)
				return true;
			return ReferenceEquals(a, b);
		}

		public static bool operator !=(JSONNode a, object b) {
			return !(a == b);
		}

		public override bool Equals(object obj) {
			return ReferenceEquals(this, obj);
		}

		#endregion operators

		internal static string Escape(string aText) {
			var result = "";
			foreach (var c in aText) {
				switch (c) {
					case '\\':
						result += "\\\\";
						break;
					case '\"':
						result += "\\\"";
						break;
					case '\n':
						result += "\\n";
						break;
					case '\r':
						result += "\\r";
						break;
					case '\t':
						result += "\\t";
						break;
					case '\b':
						result += "\\b";
						break;
					case '\f':
						result += "\\f";
						break;
					default:
						result += c;
						break;
				}
			}
			return result;
		}

		static JSONData Numberize(string token) {
			int integer;
			if (int.TryParse(token, out integer)) {
				return new JSONData(integer);
			}

			double real;
			if (double.TryParse(token, out real)) {
				return new JSONData(real);
			}

			bool flag;
			return bool.TryParse(token, out flag) ? new JSONData(flag) : new JSONNull();
		}

		static void AddElement(JSONNode ctx, string token, string tokenName, bool tokenIsString) {
			if (tokenIsString) {
				if (ctx is JSONArray)
					ctx.Add(token);
				else
					ctx.Add(tokenName, token); // assume dictionary/object
			}
			else {
				var number = Numberize(token);
				if (ctx is JSONArray)
					ctx.Add(number);
				else
					ctx.Add(tokenName, number);

			}
		}

		public static JSONNode Parse(string aJSON) {
			var stack = new Stack<JSONNode>();
			JSONNode ctx = null;
			var i = 0;
			var token = new StringBuilder();
			var tokenName = new StringBuilder();
			var quoteMode = false;
			var tokenIsString = false;
			while (i < aJSON.Length) {
				switch (aJSON[i]) {
					case '{':
						if (quoteMode) {
							token.Append(aJSON[i]);
							break;
						}
						stack.Push(new JSONClass());
						if (ctx != null) {
							var tmpName = tokenName.ToString().Trim();
							if (ctx is JSONArray)
								ctx.Add(stack.Peek());
							else if (tmpName != "")
								ctx.Add(tmpName, stack.Peek());
						}
						tokenName.Length = 0;
						token.Length = 0;
						ctx = stack.Peek();
						break;

					case '[':
						if (quoteMode) {
							token.Append(aJSON[i]);
							break;
						}

						stack.Push(new JSONArray());
						if (ctx != null) {
							var tmpName = tokenName.ToString().Trim();
							if (ctx is JSONArray)
								ctx.Add(stack.Peek());
							else if (tmpName != "")
								ctx.Add(tmpName, stack.Peek());
						}
						tokenName.Length = 0;
						token.Length = 0;
						ctx = stack.Peek();
						break;

					case '}':
					case ']':
						if (quoteMode) {

							token.Append(aJSON[i]);
							break;
						}
						if (stack.Count == 0)
							throw new Exception("JSON Parse: Too many closing brackets");

						stack.Pop();
						if (token.Length > 0) {
							AddElement(ctx, token.ToString(), tokenName.ToString(), tokenIsString);
							tokenIsString = false;
						}
						tokenName.Length = 0;
						token.Length = 0;
						if (stack.Count > 0)
							ctx = stack.Peek();
						break;

					case ':':
						if (quoteMode) {
							token.Append(aJSON[i]);
							break;
						}
						tokenName.Length = 0;
						tokenName.Append(token);
						token.Length = 0;
						tokenIsString = false;
						break;

					case '"':
						quoteMode ^= true;
						tokenIsString = true;
						break;

					case ',':
						if (quoteMode) {
							token.Append(aJSON[i]);
							break;
						}
						if (token.Length > 0) {
							AddElement(ctx, token.ToString(), tokenName.ToString(), tokenIsString);
						}
						tokenName.Length = 0;
						token.Length = 0;
						tokenIsString = false;
						break;

					case '\r':
					case '\n':
						break;

					case ' ':
					case '\t':
						if (quoteMode)
							token.Append(aJSON[i]);
						break;

					case '\\':
						++i;
						if (quoteMode) {
							var c = aJSON[i];
							switch (c) {
								case 't':
									token.Append('\t');
									break;
								case 'r':
									token.Append('\r');
									break;
								case 'n':
									token.Append('\n');
									break;
								case 'b':
									token.Append('\b');
									break;
								case 'f':
									token.Append('\f');
									break;
								case 'u': {
										var s = aJSON.Substring(i + 1, 4);
										token.Append((char)int.Parse(
											s,
											NumberStyles.AllowHexSpecifier));
										i += 4;
										break;
									}
								default:
									token.Append(c);
									break;
							}
						}
						break;

					default:
						token.Append(aJSON[i]);
						break;
				}
				++i;
			}
			if (quoteMode) {
				throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
			}
			return ctx;
		}

		public virtual void Serialize(BinaryWriter aWriter) {
		}

		public void SaveToStream(Stream aData) {
			var w = new BinaryWriter(aData);
			Serialize(w);
		}

#if USE_SharpZipLib
		public void SaveToCompressedStream(System.IO.Stream aData)
		{
			using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
			{
				gzipOut.IsStreamOwner = false;
				SaveToStream(gzipOut);
				gzipOut.Close();
			}
		}
 
		public void SaveToCompressedFile(string aFileName)
		{
 
#if USE_FileIO
			System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
			using(var F = System.IO.File.OpenWrite(aFileName))
			{
				SaveToCompressedStream(F);
			}
 
#else
			throw new Exception("Can't use File IO stuff in webplayer");
#endif
		}
		public string SaveToCompressedBase64()
		{
			using (var stream = new System.IO.MemoryStream())
			{
				SaveToCompressedStream(stream);
				stream.Position = 0;
				return System.Convert.ToBase64String(stream.ToArray());
			}
		}
 
#else
		public void SaveToCompressedStream(Stream aData) {
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public void SaveToCompressedFile(string aFileName) {
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public string SaveToCompressedBase64() {
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}
#endif

		public void SaveToFile(string aFileName){
#if USE_FileIO
			var directoryInfo = (new FileInfo(aFileName)).Directory;
			if (directoryInfo != null)
				Directory.CreateDirectory(directoryInfo.FullName);
			using (var f = File.OpenWrite(aFileName)) {
				SaveToStream(f);
			}
#else
			throw new Exception ("Can't use File IO stuff in webplayer");
#endif
		}

		public string SaveToBase64() {
			using (var stream = new MemoryStream()) {
				SaveToStream(stream);
				stream.Position = 0;
				return Convert.ToBase64String(stream.ToArray());
			}
		}

		public static JSONNode Deserialize(BinaryReader aReader) {
			var type = (JSONBinaryTag)aReader.ReadByte();
			switch (type) {
				case JSONBinaryTag.Array: {
						var count = aReader.ReadInt32();
						var tmp = new JSONArray();
						for (var i = 0; i < count; i++)
							tmp.Add(Deserialize(aReader));
						return tmp;
					}
				case JSONBinaryTag.Class: {
						var count = aReader.ReadInt32();
						var tmp = new JSONClass();
						for (var i = 0; i < count; i++) {
							var key = aReader.ReadString();
							var val = Deserialize(aReader);
							tmp.Add(key, val);
						}
						return tmp;
					}
				case JSONBinaryTag.Value: {
						return new JSONData(aReader.ReadString());
					}
				case JSONBinaryTag.IntValue: {
						return new JSONData(aReader.ReadInt32());
					}
				case JSONBinaryTag.DoubleValue: {
						return new JSONData(aReader.ReadDouble());
					}
				case JSONBinaryTag.BoolValue: {
						return new JSONData(aReader.ReadBoolean());
					}
				case JSONBinaryTag.FloatValue: {
						return new JSONData(aReader.ReadSingle());
					}

				default: {
						throw new Exception("Error deserializing JSON. Unknown tag: " + type);
					}
			}
		}

#if USE_SharpZipLib
		public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
		{
			var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
			return LoadFromStream(zin);
		}
		public static JSONNode LoadFromCompressedFile(string aFileName)
		{
#if USE_FileIO
			using(var F = System.IO.File.OpenRead(aFileName))
			{
				return LoadFromCompressedStream(F);
			}
#else
			throw new Exception("Can't use File IO stuff in webplayer");
#endif
		}
		public static JSONNode LoadFromCompressedBase64(string aBase64)
		{
			var tmp = System.Convert.FromBase64String(aBase64);
			var stream = new System.IO.MemoryStream(tmp);
			stream.Position = 0;
			return LoadFromCompressedStream(stream);
		}
#else
		public static JSONNode LoadFromCompressedFile(string aFileName) {
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public static JSONNode LoadFromCompressedStream(Stream aData) {
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public static JSONNode LoadFromCompressedBase64(string aBase64) {
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}
#endif

		public static JSONNode LoadFromStream(Stream aData) {
			using (var r = new BinaryReader(aData)) {
				return Deserialize(r);
			}
		}

		public static JSONNode LoadFromFile(string aFileName) {
#if USE_FileIO
			using (var f = File.OpenRead(aFileName)) {
				return LoadFromStream(f);
			}
#else
			throw new Exception ("Can't use File IO stuff in webplayer");
#endif
		}

		public static JSONNode LoadFromBase64(string aBase64) {
			var tmp = Convert.FromBase64String(aBase64);
			var stream = new MemoryStream(tmp){Position = 0};
			return LoadFromStream(stream);
		}
	}
	// End of JSONNode

	public class JSONArray : JSONNode, IEnumerable {
		private readonly List<JSONNode> _mList = new List<JSONNode>();

		public override JSONNode this[int aIndex] {
			get {
				if (aIndex < 0 || aIndex >= _mList.Count)
					return new JSONLazyCreator(this);
				return _mList[aIndex];
			}
			set {
				if (aIndex < 0 || aIndex >= _mList.Count)
					_mList.Add(value);
				else
					_mList[aIndex] = value;
			}
		}

		public override JSONNode this[string aKey] {
			get { return new JSONLazyCreator(this); }
			set { _mList.Add(value); }
		}

		public override int Count {
			get { return _mList.Count; }
		}


		public override void Add(string aKey, JSONNode aItem) {
			_mList.Add(aItem);
		}

		public override JSONNode Remove(int aIndex) {
			if (aIndex < 0 || aIndex >= _mList.Count)
				return null;
			var tmp = _mList[aIndex];
			_mList.RemoveAt(aIndex);
			return tmp;
		}

		public override JSONNode Remove(JSONNode aNode) {
			_mList.Remove(aNode);
			return aNode;
		}

		public override IEnumerable<JSONNode> Children {
			get {
				return _mList;
			}
		}

		public IEnumerator GetEnumerator(){
			return _mList.GetEnumerator();
		}

		public override string ToString() {
			var result = "[ ";
			foreach (var n in _mList) {
				if (result.Length > 2)
					result += ", ";
				result += n.ToString();
			}
			result += " ]";
			return result;
		}

		public override string ToString(string aPrefix) {
			var result = "[ ";
			foreach (var n in _mList) {
				if (result.Length > 3)
					result += ", ";
				result += "\n" + aPrefix + "   ";
				result += n.ToString(aPrefix + "   ");
			}
			result += "\n" + aPrefix + "]";
			return result;
		}

		public override string ToJSON(int prefix) {
			const string emptyString = "";
			var ret = "[";
			foreach (var n in _mList) {
				if (ret.Length > 1)
					ret += ",";
				//ret += "\n" + s;
				ret += n.ToJSON(prefix + 1);

			}
			ret += emptyString + "]";
			return ret;
		}

		public override void Serialize(BinaryWriter aWriter) {
			aWriter.Write((byte)JSONBinaryTag.Array);
			aWriter.Write(_mList.Count);
			foreach (var t in _mList){
				t.Serialize(aWriter);
			}
		}
	}
	// End of JSONArray

	public class JSONClass : JSONNode, IEnumerable {
		private readonly Dictionary<string, JSONNode> _mDict = new Dictionary<string, JSONNode>();

		public override JSONNode this[string aKey] {
			get {
				return _mDict.ContainsKey(aKey) ? _mDict[aKey] : new JSONLazyCreator(this, aKey);
			}
			set {
				if (_mDict.ContainsKey(aKey))
					_mDict[aKey] = value;
				else
					_mDict.Add(aKey, value);
			}
		}

		public override JSONNode this[int aIndex] {
			get {
				if (aIndex < 0 || aIndex >= _mDict.Count)
					return null;
				return _mDict.ElementAt(aIndex).Value;
			}
			set {
				if (aIndex < 0 || aIndex >= _mDict.Count)
					return;
				var key = _mDict.ElementAt(aIndex).Key;
				_mDict[key] = value;
			}
		}

		public override int Count {
			get { return _mDict.Count; }
		}

		public override void Add(string aKey, JSONNode aItem) {
			if (!string.IsNullOrEmpty(aKey)) {
				if (_mDict.ContainsKey(aKey))
					_mDict[aKey] = aItem;
				else
					_mDict.Add(aKey, aItem);
			}
			else
				_mDict.Add(Guid.NewGuid().ToString(), aItem);
		}

		public override JSONNode Remove(string aKey) {
			if (!_mDict.ContainsKey(aKey))
				return null;
			var tmp = _mDict[aKey];
			_mDict.Remove(aKey);
			return tmp;
		}

		public override JSONNode Remove(int aIndex) {
			if (aIndex < 0 || aIndex >= _mDict.Count)
				return null;
			var item = _mDict.ElementAt(aIndex);
			_mDict.Remove(item.Key);
			return item.Value;
		}

		public override JSONNode Remove(JSONNode aNode) {
			try {
				var item = _mDict.First(k => k.Value == aNode);
				_mDict.Remove(item.Key);
				return aNode;
			}
			catch {
				return null;
			}
		}

		public override IEnumerable<JSONNode> Children {
			get {
				return _mDict.Select(n => n.Value);
			}
		}

		public IEnumerator GetEnumerator(){
			return _mDict.GetEnumerator();
		}

		public override string ToString() {
			var result = "{";
			foreach (var n in _mDict) {
				if (result.Length > 2)
					result += ", ";
				result += "\"" + Escape(n.Key) + "\":" + n.Value;
			}
			result += "}";
			return result;
		}

		public override string ToString(string aPrefix) {
			var result = "{ ";
			foreach (var n in _mDict) {
				if (result.Length > 3)
					result += ", ";
				result += "\n" + aPrefix + "   ";
				result += "\"" + Escape(n.Key) + "\" : " + n.Value.ToString(aPrefix + "   ");
			}
			result += "\n" + aPrefix + "}";
			return result;
		}

		public override string ToJSON(int prefix) {
			const string emptyString = "";
			var ret = "{";
			foreach (var n in _mDict) {
				if (ret.Length > 3)
					ret += ",";
				ret += string.Format(@"""{0}"":{1}", n.Key, n.Value.ToJSON(prefix + 1));
			}
			ret += emptyString + "}";
			return ret;
		}

		public override void Serialize(BinaryWriter aWriter) {
			aWriter.Write((byte)JSONBinaryTag.Class);
			aWriter.Write(_mDict.Count);
			foreach (var k in _mDict.Keys) {
				aWriter.Write(k);
				_mDict[k].Serialize(aWriter);
			}
		}
	}
	// End of JSONClass

	public sealed class JSONNull : JSONData{
		public JSONNull() : base(default(bool)){
			Tag = JSONBinaryTag.NullValue;
			Value = "null";
		}

		public override string ToJSON(int prefix) {
			return "null";
		}
	}

	public class JSONData : JSONNode {
		private string _mData;


		public override string Value {
			get { return _mData; }
			set {
				_mData = value;
				Tag = JSONBinaryTag.Value;
			}
		}

		public JSONData(string aData) {
			_mData = aData;
			Tag = JSONBinaryTag.Value;
		}

		public JSONData(float aData) {
			AsFloat = aData;
		}

		public JSONData(double aData) {
			AsDouble = aData;
		}

		public JSONData(bool aData) {
			AsBool = aData;
		}

		public JSONData(int aData) {
			AsInt = aData;
		}

		public override string ToString() {
			return "\"" + Escape(_mData) + "\"";
		}

		public override string ToString(string aPrefix) {
			return "\"" + Escape(_mData) + "\"";
		}

		public override string ToJSON(int prefix) {
			switch (Tag) {
				case JSONBinaryTag.DoubleValue:
				case JSONBinaryTag.FloatValue:
				case JSONBinaryTag.BoolValue:
				case JSONBinaryTag.IntValue:
					return _mData;
				case JSONBinaryTag.Value:
					return string.Format("\"{0}\"", Escape(_mData));
				default:
					throw new NotSupportedException("This shouldn't be here: " + Tag);
			}
		}

		public override void Serialize(BinaryWriter aWriter) {
			var tmp = new JSONData(""){AsInt = AsInt};

			if (tmp._mData == _mData) {
				aWriter.Write((byte)JSONBinaryTag.IntValue);
				aWriter.Write(AsInt);
				return;
			}
			tmp.AsFloat = AsFloat;
			if (tmp._mData == _mData) {
				aWriter.Write((byte)JSONBinaryTag.FloatValue);
				aWriter.Write(AsFloat);
				return;
			}
			tmp.AsDouble = AsDouble;
			if (tmp._mData == _mData) {
				aWriter.Write((byte)JSONBinaryTag.DoubleValue);
				aWriter.Write(AsDouble);
				return;
			}

			tmp.AsBool = AsBool;
			if (tmp._mData == _mData) {
				aWriter.Write((byte)JSONBinaryTag.BoolValue);
				aWriter.Write(AsBool);
				return;
			}
			aWriter.Write((byte)JSONBinaryTag.Value);
			aWriter.Write(_mData);
		}
	}
	// End of JSONData

	internal class JSONLazyCreator : JSONNode {
		private JSONNode _mNode;
		private readonly string _mKey;

		public JSONLazyCreator(JSONNode aNode) {
			_mNode = aNode;
			_mKey = null;
		}

		public JSONLazyCreator(JSONNode aNode, string aKey) {
			_mNode = aNode;
			_mKey = aKey;
		}

		private void Set(JSONNode aVal) {
			if (_mKey == null) {
				_mNode.Add(aVal);
			}
			else {
				_mNode.Add(_mKey, aVal);
			}
			_mNode = null; // Be GC friendly.
		}

		public override JSONNode this[int aIndex] {
			get {
				return new JSONLazyCreator(this);
			}
			set {
				var tmp = new JSONArray();
				tmp.Add(value);
				Set(tmp);
			}
		}

		public override JSONNode this[string aKey] {
			get {
				return new JSONLazyCreator(this, aKey);
			}
			set {
				var tmp = new JSONClass{{aKey, value}};
				Set(tmp);
			}
		}

		public override void Add(JSONNode aItem) {
			var tmp = new JSONArray();
			tmp.Add(aItem);
			Set(tmp);
		}

		public override void Add(string aKey, JSONNode aItem) {
			var tmp = new JSONClass{{aKey, aItem}};
			Set(tmp);
		}

		public static bool operator ==(JSONLazyCreator a, object b) {
			if (b == null)
				return true;
			return ReferenceEquals(a, b);
		}

		public static bool operator !=(JSONLazyCreator a, object b) {
			return !(a == b);
		}

		public override bool Equals(object obj){
			return obj == null || ReferenceEquals(this, obj);
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public override string ToString() {
			return "";
		}

		public override string ToString(string aPrefix) {
			return "";
		}

		public override string ToJSON(int prefix) {
			return "";
		}

		public override int AsInt {
			get {
				var tmp = new JSONData(0);
				Set(tmp);
				return 0;
			}
			set {
				var tmp = new JSONData(value);
				Set(tmp);
			}
		}

		public override float AsFloat {
			get {
				var tmp = new JSONData(0.0f);
				Set(tmp);
				return 0.0f;
			}
			set {
				var tmp = new JSONData(value);
				Set(tmp);
			}
		}

		public override double AsDouble {
			get {
				var tmp = new JSONData(0.0);
				Set(tmp);
				return 0.0;
			}
			set {
				var tmp = new JSONData(value);
				Set(tmp);
			}
		}

		public override bool AsBool {
			get {
				var tmp = new JSONData(false);
				Set(tmp);
				return false;
			}
			set {
				var tmp = new JSONData(value);
				Set(tmp);
			}
		}

		public override JSONArray AsArray {
			get {
				var tmp = new JSONArray();
				Set(tmp);
				return tmp;
			}
		}

		public override JSONClass AsObject {
			get {
				var tmp = new JSONClass();
				Set(tmp);
				return tmp;
			}
		}
	}
	// End of JSONLazyCreator
}