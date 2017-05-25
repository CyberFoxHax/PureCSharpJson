using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PureCSharpJson.PureCSharpJson;

namespace PureCSharpJson.Tests{
	[TestClass]
	public class JsonDeserializeTest {
		public static void AreEqual<T>(T p, string json) {
			Assert.AreEqual(p, NewJson.Deserialize<T>(json));
		}

		public static void AreNotEqual<T>(T p, string json) {
			Assert.AreNotEqual(p, NewJson.Deserialize<T>(json));
		}

		public static void AreEqualArr<T>(IEnumerable<T> p, string json) {
			var listA = p as IList;
			var listB = NewJson.Deserialize(json, p.GetType()) as IList;
			if (listA == null || listB == null) return;
			if (listA.Count != listB.Count) return;
			if (listA.Cast<object>().Where((t, i) => t.Equals(listB[i]) == false).Any() == false)
				return;
			Assert.Fail();
		}

		public static void AreNotEqualArr<T>(IEnumerable<T> p, string json) {
			var listA = p as IList;
			var listB = NewJson.Deserialize(json, p.GetType()) as IList;
			if (listA == null || listB == null) return;
			if (listA.Count != listB.Count) return;
			if (listA.Cast<object>().Where((t, i) => t.Equals(listB[i]) == false).Any())
				return;
			Assert.Fail();
		}

		public abstract class Compareable {
			public override bool Equals(object b) {
				var a = this;
				var type = a.GetType();
				if (b == null) return false;
				if (b.GetType() != type) return false;
				var any = false;
				foreach (var prop in type.GetProperties()){
					var aV = prop.GetValue(a, null);
					var bV = prop.GetValue(b, null);
					if(aV == null && bV == null)
						continue;

					var listA = aV as IList;
					var listB = bV as IList;
				
					if (listA != null && listB != null){
						if (listA.Cast<object>().Where((p, i) => Equals(p, listB[i]) == false).Any()){
							any = true;
							break;
						}
						continue;
					}
					if (Equals(aV, bV) == false){
						any = true;
						break;
					}
				}
				return any == false;
			}
		}

		[TestMethod]
		public void EqualityOperator(){
			Assert.AreEqual(
				new BasicClass {
					StringValue = "GrassMudHorse",
					IntValue = 123,
					FloatValue = 123.123f,
					BoolValue = true
				},
				new BasicClass {
					StringValue = "GrassMudHorse",
					IntValue = 123,
					FloatValue = 123.123f,
					BoolValue = true
				}
				);

			Assert.AreNotEqual(
				new BasicClass {
					StringValue = "GrassMudHorse",
					IntValue = 321,
					FloatValue = 123.123f,
					BoolValue = true
				},
				new BasicClass {
					StringValue = "NotEqual",
					IntValue = 321,
					FloatValue = 123.123f,
					BoolValue = true
				}
				);

			Assert.AreEqual(
				new NestedArraySimple { Ints = new[] { 1, 2, 3 } },
				new NestedArraySimple { Ints = new[] { 1, 2, 3 } }
				);

			Assert.AreNotEqual(
				new NestedArraySimple { Ints = new[] { 1, 2, 3 } },
				new NestedArraySimple { Ints = new[] { 1, 0, 3 } }
				);

			Assert.AreEqual(
				new NestedBasicClass{
					NestedClass = new BasicClass {
						StringValue = "Yes",
						IntValue = 321,
						FloatValue = 123.123f,
						BoolValue = true
					}
				},
				new NestedBasicClass{
					NestedClass = new BasicClass {
						StringValue = "Yes",
						IntValue = 321,
						FloatValue = 123.123f,
						BoolValue = true
					}
				}
				);
		}

		private class BasicClass : Compareable {
			public string StringValue { get; set; }
			public int IntValue { get; set; }
			public float FloatValue { get; set; }
			public bool BoolValue { get; set; }
		}

		private class NestedBasicClass : Compareable{
			public BasicClass NestedClass { get; set; }
		}

		private class NestedArraySimple : Compareable {
			public int[] Ints { get; set; }
		}

		private class NestedArrayComplex : Compareable {
			public BasicClass[] Classes { get; set; }
		}

		private class NestedArrayJagged : Compareable{
			public int[][] Ints2 { get; set; }
		}

		private class NestedList : Compareable{
			public List<int> Ints { get; set; }
		}

		private class GenericClass<T> : Compareable{
			public T Prop { get; set; }
		}

		private class IgnoredProperties : Compareable{
			public int NotIgnored { get; set; }
			[ScriptIgnore]
			public int TotallyIgnored { get; set; }
		}

		[TestMethod]
		public void SimpleClass(){
			var obj = new BasicClass{
				StringValue = "GrassMudHorse",
				IntValue = 123,
				FloatValue = 123.123f,
				BoolValue = true
			};

			AreEqual(obj, @"{""StringValue"":""GrassMudHorse"",""IntValue"":123,""FloatValue"":123.123,""BoolValue"":true}"); // compact
			AreEqual(obj, @"{ ""StringValue"" : ""GrassMudHorse"", ""IntValue"" : 123, ""FloatValue"" : 123.123, ""BoolValue"" : true }"); // spaces
			AreEqual(obj,
				@"   {""StringValue"":""GrassMudHorse"",""IntValue"":123    \r\n,  \t\t\t""FloatValue""   :123.123,   \n""BoolValue"":true    }"); // fucked
		}

		[TestMethod]
		public void ClassOmittedProperties() {
			AreEqual(new BasicClass {
				StringValue = "GrassMudHorse",
				IntValue = 0,
				FloatValue = 123.123f,
				BoolValue = true
			}, @"{""StringValue"":""GrassMudHorse"",""FloatValue"":123.123,""BoolValue"":true}");

			AreEqual(new BasicClass {
				StringValue = "GrassMudHorse",
				IntValue = 123,
				FloatValue = 123.123f,
				BoolValue = true
			}, @"{""StringValue"":""GrassMudHorse"",""IntValue"":123,""FloatValue"":123.123,""BoolValue"":true}");
		}

		[TestMethod]
		public void NestedClass(){
			var obj = new NestedBasicClass{
				NestedClass = new BasicClass {
					StringValue = "NotEqual",
					IntValue = 321,
					FloatValue = 123.123f,
					BoolValue = true
				}
			};
			AreEqual(obj, @"{""NestedClass"":{""StringValue"":""NotEqual"",""IntValue"":321,""FloatValue"":123.123,""BoolValue"":true}}");
			obj.NestedClass = new BasicClass();
			AreEqual(obj, @"{""NestedClass"":{}}");
			obj.NestedClass = null;
			AreEqual(obj, @"{}");
			AreEqual(obj, @"{""NestedClass"":null}");
		}

		[TestMethod]
		public void NestedArray() {
			AreEqual(new NestedArraySimple {
				Ints = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }
			}, @"{""Ints"":[1,2,3,4,5,6,7,8,9]}");

			AreNotEqual(new NestedArraySimple {
				Ints = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }
			}, @"{""Ints"":[1,2,3,4,5,1231]}");
		}

		[TestMethod]
		public void NestedNullArray() {
			AreEqual(new NestedArraySimple {
				Ints = null
			}, @"{""Ints"":null}");
		}

		[TestMethod]
		public void NestedNullArrayEntries() {
			AreEqual(new NestedArraySimple {
				Ints = new[] { 1, 0, 3, 4, 0, 6, 7, 0, 9 }
			}, @"{""Ints"":[1,null,3,4,null,6,7,null,9]}");
		}

		[TestMethod]
		public void NestedNullArrayEntriesComplex() {
			AreEqual(new NestedArrayComplex {
				Classes = new[] {
					null,
					new BasicClass {
						StringValue = "Indice1",
						IntValue = 123,
						FloatValue = 123.123f,
						BoolValue = true
					},
					null,
					new BasicClass {
						StringValue = "Indice2",
						IntValue = 456,
						FloatValue = 456.456f,
						BoolValue = true
					} ,
					null
				}
			},
				@"{""Classes"":[null,{""StringValue"":""Indice1"",""IntValue"":123,""FloatValue"":123.123,""BoolValue"":true},null,{""StringValue"":""Indice2"",""IntValue"":456,""FloatValue"":456.456,""BoolValue"":true},null]}");
		}


		[TestMethod]
		public void ArrayPrimitive() {
			AreEqualArr(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "[1,2,3,4,5,6,7,8,9]");
			AreNotEqualArr(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "[1,2,3,4,5,6,7,8,0]");
			AreEqualArr(new[] { 123, 234, 345, 456 }, "[123,234,345,456]");
			AreEqualArr(new[] { "hello", "world", "!" }, @"[""hello"",""world"",""!""]");

			AreNotEqualArr(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "[1,2,3,4,5]");
			AreNotEqualArr(new[] { 1, 2, 3, 4, 5 }, "[1,2,3,4,5,6,7,8,9]");
		}

		[TestMethod]
		public void ArrayClass() {
			AreEqualArr(new[]{
				new BasicClass {
					StringValue = "Indice1",
					IntValue = 123,
					FloatValue = 123.123f,
					BoolValue = true
				},
				new BasicClass {
					StringValue = "Indice2",
					IntValue = 456,
					FloatValue = 456.456f,
					BoolValue = true
				}
			}, @"[{""StringValue"":""Indice1"",""IntValue"":123,""FloatValue"":123.123,""BoolValue"":true},{""StringValue"":""Indice2"",""IntValue"":456,""FloatValue"":456.456,""BoolValue"":true}]");
		}

		[TestMethod]
		public void ArrayNestedJagged(){
			var jaggedArr = new NestedArrayJagged{
				Ints2 = new[]{
					new[]{1, 2, 3},
					new[]{4, 5, 6},
					new[]{7, 8, 9}
				}
			};

			var json = NewJson.Deserialize<NestedArrayJagged>(@"{""Ints2"":[[1,2,3],[4,5,6],[7,8,9]]}");

			for (var i = 0; i < 3; i++)
				for (var ii = 0; ii < 3; ii++)
					Assert.AreEqual(jaggedArr.Ints2[i][ii], json.Ints2[i][ii]);
		}

		[TestMethod]
		public void ArrayJagged() {
			var jaggedArr = new[]{
				new[]{1, 2, 3},
				new[]{4, 5, 6},
				new[]{7, 8, 9}
			};

			var json = NewJson.Deserialize<int[][]>(@"[[1,2,3],[4,5,6],[7,8,9]]");

			for (var i = 0; i < 3; i++)
				for (var ii = 0; ii < 3; ii++)
					Assert.AreEqual(jaggedArr[i][ii], json[i][ii]);
		}

		[TestMethod]
		public void GenericClassPrimitive() {
			AreEqual(new GenericClass<int> {
				Prop = 123
			},
				@"{""Prop"":123}");
		}

		[TestMethod]
		public void GenericClassArray() {
			AreEqual(new GenericClass<int[]> {
				Prop = new[] { 1, 2, 3 }
			},
				@"{""Prop"":[1,2,3]}");
		}

		[TestMethod]
		public void ListNested() {
			AreEqual(new NestedList {
				Ints = new List<int>{ 1, 2, 3 }
			},
				@"{""Ints"":[1,2,3]}");
		}

		[TestMethod]
		public void ListBasic() {
			AreEqualArr(new List<int> {
				1, 2, 3
			},
				@"[1,2,3]");
		}

		[TestMethod]
		public void ScriptIgnore() {
			AreEqual(new IgnoredProperties {
				NotIgnored = 123,
				TotallyIgnored = 0
			},
				@"{""NotIgnored"":123}");

			AreEqual(new IgnoredProperties {
				NotIgnored = 123,
				TotallyIgnored = 0
			},
				@"{""NotIgnored"":123, ""TotallyIgnored"":123}");
		}

		[TestMethod]
		public void GenericClassComplex() {
			AreEqual(new GenericClass<BasicClass> {
				Prop = new BasicClass {
					StringValue = "ComplexTypeYumYum",
					IntValue = 123,
					FloatValue = 123.123f,
					BoolValue = true
				}
			},
				@"{""Prop"":{""StringValue"":""ComplexTypeYumYum"",""IntValue"":123,""FloatValue"":123.123,""BoolValue"":true}}");
		}

		[TestMethod]
		public void SerializeDeserialize(){
			var obj = new BasicClass{
				StringValue = "GrassMudHorse",
				IntValue = 123,
				FloatValue = 123.123f,
				BoolValue = true
			};
			Assert.AreEqual(
				obj,
				NewJson.Deserialize<BasicClass>(NewJson.Serialize(obj))
				);
		}
	}
}
