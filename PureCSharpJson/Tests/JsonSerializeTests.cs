using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PureCSharpJson.PureCSharpJson;

namespace PureCSharpJson.Tests{
	[TestClass]
	public class JsonSerializerTests {
		private class BasicClass {
			public int IntProp { get; set; }
			public double DoubleProp { get; set; }
			public float FloatProp { get; set; }
		}

		private class WithAnon {
			public object Anon { get; set; }
		}

		private class MixedFields {
			public int Prop { get; set; }
			public int Field;
		}

		private class IgnoredProps {
			public int NotIgnored { get; set; }
			[ScriptIgnore]
			public int Ignored { get; set; }
		}

		private class StringObject {
			public string StringValue { get; set; }
		}

		private class NestedClass {
			public BasicClass Class { get; set; }
		}

		private class NestedClass2 {
			public NestedClass Class { get; set; }
		}

		private class NestedClassWithNullAndProp {
			public NestedClass Class { get; set; }
			public int Value { get; set; }
		}

		private class Generic<T> {
			public T Property { get; set; }
		}

		private enum TestEnum{
			Key1Default,
			Key2,
			Key3,
		}

		[TestMethod]
		public void GenericBasic() {
			Assert.AreEqual(
				@"{""Property"":{""IntProp"":123,""DoubleProp"":0.123123123,""FloatProp"":0.123123}}",
				NewJson.Serialize(new Generic<BasicClass> { Property = new BasicClass { IntProp = 123, DoubleProp = 0.123123123, FloatProp = 0.123123f } })
				);
		}

		[TestMethod]
		public void GenericPrimitive() {
			Assert.AreEqual(
				@"{""Property"":""HelloJSON!""}",
				NewJson.Serialize(new Generic<string> { Property = "HelloJSON!"})
				);
		}

		[TestMethod]
		public void GenericList() {
			Assert.AreEqual(
				@"{""Property"":[1,2,3,4,5,6]}",
				NewJson.Serialize(new Generic<List<int>> { Property = new List<int>{1,2,3,4,5,6} })
				);
		}

		[TestMethod]
		public void GenericRecurse() {
			Assert.AreEqual(
				@"{""Property"":{""Property"":""SuchRecursion""}}",
				NewJson.Serialize(new Generic<Generic<string>> { Property = new Generic<string> { Property = "SuchRecursion" } })
				);
		}

		[TestMethod]
		public void GenericComplexRecurse() {
			Assert.AreEqual(
				@"{""Property"":{""Property"":{""IntProp"":123,""DoubleProp"":0.123123123,""FloatProp"":0.123123}}}",
				NewJson.Serialize(new Generic<Generic<BasicClass>> { Property = new Generic<BasicClass> { Property = new BasicClass { IntProp = 123, DoubleProp = 0.123123123, FloatProp = 0.123123f } } })
				);
		}

		[TestMethod]
		public void BasicObject() {
			Assert.AreEqual(
				@"{""IntProp"":123,""DoubleProp"":0.123123123,""FloatProp"":0.123123}",
				NewJson.Serialize(new BasicClass { IntProp = 123, DoubleProp = 0.123123123, FloatProp = 0.123123f })
				);
		}

		[TestMethod]
		public void MixWithFields() {
			Assert.AreEqual(
				@"{""Prop"":123}",
				NewJson.Serialize(new MixedFields { Field = 2, Prop = 123 })
				);
		}

		[TestMethod]
		public void NestedClasses() {
			Assert.AreEqual(
				@"{""Class"":{""IntProp"":1234}}",
				NewJson.Serialize(new NestedClass { Class = new BasicClass { IntProp = 1234 } })
				);
		}

		[TestMethod]
		public void Enums() {
			Assert.AreEqual(
				@"{""Val2"":1,""Val3"":2}",
				NewJson.Serialize(new { Val1 = TestEnum.Key1Default, Val2 = TestEnum.Key2, Val3 = TestEnum.Key3 })
				);
		}

		[TestMethod]
		public void OmmitNullRecurisve1() {
			Assert.Inconclusive();
			Assert.AreEqual(
				@"{}",
				NewJson.Serialize(new NestedClass { Class = null })
				);
		}

		[TestMethod]
		public void OmmitNullRecursive2() {
			Assert.Inconclusive();
			Assert.AreEqual(
				@"{""Class"":{}}",
				NewJson.Serialize(new NestedClass2{
					Class = new NestedClass{
						Class = null
					}
				} )
				);
		}

		[TestMethod]
		public void OmmitNullClassProperties() {
			Assert.Inconclusive();
			Assert.AreEqual(
				@"{""Class"":{}, ""Value"":3}",
				NewJson.Serialize(new NestedClassWithNullAndProp {
					Class = new NestedClass {
						Class = null
					},
					Value = 3
				})
				);
		}

		[TestMethod]
		public void Null() {
			Assert.AreEqual(
				@"{}",
				NewJson.Serialize(null)
				);
		}

		[TestMethod]
		public void StringObjects() {
			Assert.AreEqual(
				@"{""StringValue"":""FuckMother""}",
				NewJson.Serialize(new StringObject { StringValue = "FuckMother"})
				);
		}

		[TestMethod]
		public void ScriptIgnore() {
			Assert.AreEqual(
				@"{""NotIgnored"":2}",
				NewJson.Serialize(new IgnoredProps{ NotIgnored = 2, Ignored = 123 })
				);
		}

		[TestMethod]
		public void AnonymousObject() {
			Assert.AreEqual(
				@"{""IntProp"":123,""DoubleProp"":0.123123123,""FloatProp"":0.123123}",
				NewJson.Serialize(new { IntProp = 123, DoubleProp = 0.123123123, FloatProp = 0.123123f })
				);
		}

		[TestMethod]
		public void AnonymousObjectWithNestedBasicObject() {
			Assert.AreEqual(
				@"{""ClassProp"":{""DoubleProp"":123}}",
				NewJson.Serialize(new { ClassProp = new BasicClass { DoubleProp = 123 } })
				);
		}

		[TestMethod]
		public void AnonymousObjectWithNestedAnonymousObject() {
			Assert.AreEqual(
				@"{""ClassProp"":{""Val"":123}}",
				NewJson.Serialize(new { ClassProp = new { Val = 123 } })
				);
		}

		[TestMethod]
		public void BasicObjectWithNestedAnonymousObject() {
			Assert.AreEqual(
				@"{""Anon"":{""Val"":123}}",
				NewJson.Serialize(new WithAnon { Anon = new { Val = 123 } })
				);
		}

		[TestMethod]
		public void SimpleNumberArray() {
			Assert.AreEqual(
				@"[1,2,3,4,5,6,7,8,9]",
				NewJson.Serialize(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
				);
		}

		[TestMethod]
		public void SimpleNumberArray2() {
			Assert.AreEqual(
				@"[123456789,123456789,123456789]",
				NewJson.Serialize(new[] { 123456789, 123456789, 123456789 })
				);
		}

		[TestMethod]
		public void SimpleNumberList() {
			Assert.AreEqual(
				@"[1,2,3,4,5,6,7,8,9]",
				NewJson.Serialize(new List<float> { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
				);
		}

		[TestMethod]
		public void SimpleStringArray() {
			Assert.AreEqual(
				@"[""Hello"",""World"",""!""]",
				NewJson.Serialize(new []{ "Hello", "World", "!" })
				);
		}

		[TestMethod]
		public void ArrayOfAnonymousObjects() {
			Assert.AreEqual(
				@"[{""Val"":1},{""Val"":2},{""Val"":3}]",
				NewJson.Serialize(new[] { new { Val = 1 }, new { Val = 2 }, new { Val = 3 } })
				);
		}

		[TestMethod]
		public void ArrayWithNull() {
			Assert.AreEqual(
				@"[null,null,{""Val"":3}]",
				NewJson.Serialize(new[] { null, null, new { Val = 3 } })
				);
		}

		[TestMethod]
		public void ArrayOfBasicObjectsOnDifferentProperties() {
			Assert.AreEqual(
				@"[{""IntProp"":1},{""DoubleProp"":2},{""FloatProp"":3}]",
				NewJson.Serialize(new[] { new BasicClass { IntProp = 1 }, new BasicClass { DoubleProp = 2 }, new BasicClass { FloatProp = 3 } })
				);
		}
	}
}
