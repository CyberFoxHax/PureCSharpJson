# PureCSharpJson
A simple reflection based json system with no dll requirements, initially meant for a replacement to Unity's shabby json systems.

It includes a modified version of SimpleJSON adjusted to support the Null data type

### Serializing usage
```CSharp
PureCSharpJson.Serialize(new { Numba = 1234, Str = "Hey there!" });

// {"Numba":1234,"Str":"Hey there!"}
```
### Deserializing usage
```CSharp
class Model {
    public string StringValue { get; set; }
    public int SomeNumber { get; set; }
}
PureCSharpJson.Deserialize<Model>("{\"StringValue\":\"Value\",\"SomeNumber\",12345}");
    
// Model.StringValue == "Value"
// Model.SomeNumber == 12345
```
   
### Features
 * Anonymous classes
 * Arrays and Lists
 * Jagged arrays (only tested for 2 dimensions)
 * ScriptIgnore Attribute to ommit properties from being handled
 * Not failing on oddly formatted JSON. Filtering out. Whitespace, \r, \n and  \t
 
#### Notes
 * Only handles Properties (Not Fields, contrary to Unity3d's JSON implementation)
 * Null properties will be omitted when serialized
 * Null properties will deserialize to `null` on Complex types and deserialize to `default(T)` on Value types
 * In Complex arrays, null values will be serialized like so: [null,{},null,null,{}] to preserve indexes
 * In Value arrays, null values will be serialized like so: [0,123,0,0,123] to preserve indexes
 
#### Todo
- [ ] Ommit value properties with default value
- [ ] 3 tests are failing
