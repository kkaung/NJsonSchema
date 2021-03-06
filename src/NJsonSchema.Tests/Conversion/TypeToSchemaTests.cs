﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Conversion
{
    [TestClass]
    public class TypeToSchemaTests
    {
        [TestMethod]
        public async Task When_converting_in_round_trip_then_json_should_be_the_same()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Act
            var schemaData1 = JsonConvert.SerializeObject(schema, Formatting.Indented);
            var schema2 = JsonConvert.DeserializeObject<JsonSchema4>(schemaData1);
            var schemaData2 = JsonConvert.SerializeObject(schema2, Formatting.Indented);

            //// Assert
            Assert.AreEqual(schemaData1, schemaData2);
        }

        [TestMethod]
        public async Task When_converting_simple_property_then_property_must_be_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();
            var data = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer, schema.Properties["Integer"].Type);
            Assert.AreEqual(JsonObjectType.Number, schema.Properties["Decimal"].Type);
            Assert.AreEqual(JsonObjectType.Number, schema.Properties["Double"].Type);
            Assert.AreEqual(JsonObjectType.Boolean, schema.Properties["Boolean"].Type);
            Assert.AreEqual(JsonObjectType.String | JsonObjectType.Null, schema.Properties["String"].Type);
            Assert.AreEqual(JsonObjectType.Array | JsonObjectType.Null, schema.Properties["Array"].Type);
        }

        [TestMethod]
        public async Task When_converting_nullable_simple_property_then_property_must_be_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer | JsonObjectType.Null, schema.Properties["NullableInteger"].Type);
            Assert.AreEqual(JsonObjectType.Number | JsonObjectType.Null, schema.Properties["NullableDecimal"].Type);
            Assert.AreEqual(JsonObjectType.Number | JsonObjectType.Null, schema.Properties["NullableDouble"].Type);
            Assert.AreEqual(JsonObjectType.Boolean | JsonObjectType.Null, schema.Properties["NullableBoolean"].Type);
        }

        [TestMethod]
        public async Task When_converting_property_with_description_then_description_should_be_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.AreEqual("Test", schema.Properties["Integer"].Description);
        }

        [TestMethod]
        public async Task When_converting_required_property_then_it_should_be_required_in_schema()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.IsTrue(schema.Properties["RequiredReference"].IsRequired);
        }

        [TestMethod]
        public async Task When_converting_regex_property_then_it_should_be_set_as_pattern()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.AreEqual("regex", schema.Properties["RegexString"].Pattern);
        }

        public class ClassWithRegexDictionaryProperty
        {
            [RegularExpression("^\\d+\\.\\d+\\.\\d+\\.\\d+$")]
            public Dictionary<string, string> Versions { get; set; }
        }

        [TestMethod]
        public async Task When_dictionary_property_has_regex_attribute_then_regex_is_added_to_additionalProperties()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithRegexDictionaryProperty>();
            var json = schema.ToJson();

            //// Assert
            Assert.IsNull(schema.Properties["Versions"].Pattern);
            Assert.IsNotNull(schema.Properties["Versions"].AdditionalPropertiesSchema.ActualSchema.Pattern);
        }

        [TestMethod]
        public async Task When_converting_range_property_then_it_should_be_set_as_min_max()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.AreEqual(5, schema.Properties["RangeInteger"].Minimum);
            Assert.AreEqual(10, schema.Properties["RangeInteger"].Maximum);
        }

        [TestMethod]
        public async Task When_converting_not_nullable_properties_then_they_should_have_null_type()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.IsFalse(schema.Properties["Integer"].IsRequired);
            Assert.IsFalse(schema.Properties["Decimal"].IsRequired);
            Assert.IsFalse(schema.Properties["Double"].IsRequired);
            Assert.IsFalse(schema.Properties["Boolean"].IsRequired);
            Assert.IsFalse(schema.Properties["String"].IsRequired);

            Assert.IsFalse(schema.Properties["Integer"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsFalse(schema.Properties["Decimal"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsFalse(schema.Properties["Double"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsFalse(schema.Properties["Boolean"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsTrue(schema.Properties["String"].Type.HasFlag(JsonObjectType.Null));
        }

        [TestMethod]
        public async Task When_generating_nullable_primitive_properties_then_they_should_have_null_type()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.IsTrue(schema.Properties["NullableInteger"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsTrue(schema.Properties["NullableDecimal"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsTrue(schema.Properties["NullableDouble"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsTrue(schema.Properties["NullableBoolean"].Type.HasFlag(JsonObjectType.Null));
        }

        [TestMethod]
        public async Task When_property_is_renamed_then_the_name_must_be_correct()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("abc"));
            Assert.IsFalse(schema.Properties.ContainsKey("ChangedName"));
        }

        [TestMethod]
        public async Task When_converting_object_then_it_should_be_correct()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();
            var data = schema.ToJson();

            //// Assert
            var property = schema.Properties["Reference"];
            Assert.IsTrue(property.IsNullable(SchemaType.JsonSchema));
            Assert.IsTrue(schema.Definitions.Any(d => d.Key == "MySubtype"));
        }
        
        [TestMethod]
        public async Task When_converting_enum_then_enum_array_must_be_set()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var property = schema.Properties["Color"];

            //// Assert
            Assert.AreEqual(3, property.ActualTypeSchema.Enumeration.Count); // Color property has StringEnumConverter
            Assert.IsTrue(property.ActualTypeSchema.Enumeration.Contains("Red"));
            Assert.IsTrue(property.ActualTypeSchema.Enumeration.Contains("Green"));
            Assert.IsTrue(property.ActualTypeSchema.Enumeration.Contains("Blue"));
        }

        public class ClassWithJObjectProperty
        {
            public JObject Property { get; set; }
        }

        [TestMethod]
        public async Task When_type_is_JObject_then_generated_type_is_any()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithJObjectProperty>();
            var schemaData = schema.ToJson();
            var property = schema.Properties["Property"];

            //// Assert
            Assert.IsTrue(property.IsNullable(SchemaType.JsonSchema));
            Assert.IsTrue(property.ActualTypeSchema.IsAnyType);
            Assert.IsTrue(property.ActualTypeSchema.AllowAdditionalItems);
            Assert.AreEqual(0, property.Properties.Count);
        }

        [TestMethod]
        public async Task When_converting_array_then_items_must_correctly_be_loaded()
        {
            await When_converting_array_then_items_must_correctly_be_loaded("Array");
        }

        [TestMethod]
        public async Task When_converting_collection_then_items_must_correctly_be_loaded()
        {
            await When_converting_array_then_items_must_correctly_be_loaded("Collection");
        }

        [TestMethod]
        public async Task When_converting_list_then_items_must_correctly_be_loaded()
        {
            await When_converting_array_then_items_must_correctly_be_loaded("List");
        }

        public async Task When_converting_array_then_items_must_correctly_be_loaded(string propertyName)
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            //// Assert
            var property = schema.Properties[propertyName];

            Assert.AreEqual(JsonObjectType.Array | JsonObjectType.Null, property.Type);
            Assert.AreEqual(JsonObjectType.Object, property.ActualSchema.Item.ActualSchema.Type);
            Assert.IsTrue(schema.Definitions.Any(d => d.Key == "MySubtype"));
            Assert.AreEqual(JsonObjectType.String | JsonObjectType.Null, property.ActualSchema.Item.ActualSchema.Properties["Id"].Type);
        }
        
        [TestMethod]
        public async Task When_converting_object_with_display_name_then_schema_title_should_be_set()
        {
            var schema = await JsonSchema4.FromTypeAsync<MyType>();

            Assert.AreEqual("My Type", schema.Title);
        }

        [TestMethod]
        public async Task When_converting_object_with_empty_display_name_then_fallback_to_reflection()
        {
            var schema = await JsonSchema4.FromTypeAsync<FallBackDisplayName>();

            Assert.AreEqual(nameof(FallBackDisplayName), schema.Title);
        }

        [System.ComponentModel.DisplayName]
        public class FallBackDisplayName
        {
        }

        [System.ComponentModel.DisplayName("My Type")]
        public class MyType
        {
            [System.ComponentModel.Description("Test")]
            public int Integer { get; set; }
            public decimal Decimal { get; set; }
            public double Double { get; set; }
            public bool Boolean { get; set; }

            public int? NullableInteger { get; set; }
            public decimal? NullableDecimal { get; set; }
            public double? NullableDouble { get; set; }
            public bool? NullableBoolean { get; set; }

            public string String { get; set; }

            [JsonProperty("abc")]
            public string ChangedName { get; set; }

            [Required]
            public MySubtype RequiredReference { get; set; }

            [RegularExpression("regex")]
            public string RegexString { get; set; }

            [Range(5, 10)]
            public int RangeInteger { get; set; }

            public MySubtype Reference { get; set; }
            public MySubtype[] Array { get; set; }
            public Collection<MySubtype> Collection { get; set; }
            public List<MySubtype> List { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public MyColor Color { get; set; }
        }

        public class MySubtype
        {
            public string Id { get; set; }
        }

        public enum MyColor
        {
            Red,
            Green,
            Blue
        }
    }
}
