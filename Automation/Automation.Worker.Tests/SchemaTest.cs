using Automation.Models.Schema;
using Automation.Worker.Packages;
using System.Runtime.Serialization;

namespace Automation.Worker.Tests
{
    public class SchemaClassTest
    {
        public string StringMember { get; set; } = "";
        public bool BoolMember { get; set; }
        public int IntMember { get; set; }
        public DateTime DateTimeMember { get; set; }

        public SchemaSubClassTest SubProperty { get; set; } = new SchemaSubClassTest();
        public List<string> StringList { get; set; } = [];
        public List<SchemaSubClassTest> TableList { get; set; } = [];
    }

    public class SchemaSubClassTest
    {
        public string SubStringMember { get; set; } = "";
        public bool SubBoolMember { get; set; }
        public int SubIntMember { get; set; }
        public DateTime SubDateTimeMember { get; set; }

        [IgnoreDataMember]
        public bool Ignored { get; set; }
    }

    public class SchemaTest
    {
        [Fact]
        public void ConvertValue()
        {
            ISchemaElement element = SchemaFactory.Convert(typeof(bool));
            Assert.IsType<SchemaTypedValue>(element);
            SchemaTypedValue value = (SchemaTypedValue)element;
            Assert.Equal(EnumDataType.Boolean, value.DataType);
        }

        [Fact]
        public void ConvertArray()
        {
            ISchemaElement element = SchemaFactory.Convert(typeof(List<string>));
            Assert.IsType<SchemaArray>(element);
            SchemaArray array = (SchemaArray)element;
            Assert.IsType<SchemaPropertyValue>(array.Schema);
            SchemaPropertyValue property = (SchemaPropertyValue)array.Schema;
            Assert.Equal(EnumDataType.String, ((SchemaTypedValue)property.Element).DataType);
        }

        [Fact]
        public void ConvertValueObject()
        {
            ISchemaElement element = SchemaFactory.Convert(typeof(SchemaSubClassTest));
            Assert.IsType<SchemaObject>(element);
            SchemaObject schema = (SchemaObject)element;

            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaSubClassTest.SubStringMember));
            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaSubClassTest.SubBoolMember));
            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaSubClassTest.SubIntMember));
            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaSubClassTest.SubDateTimeMember));

            Assert.DoesNotContain(schema.Properties, prop => prop.Name == nameof(SchemaSubClassTest.Ignored));
        }

        [Fact]
        public void ConvertSubObject()
        {
            ISchemaElement element = SchemaFactory.Convert(typeof(SchemaClassTest));
            Assert.IsType<SchemaObject>(element);
            SchemaObject schema = (SchemaObject)element;

            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaClassTest.SubProperty));
            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaClassTest.TableList));

            ISchemaObjectProperty? subProperty = schema[nameof(SchemaClassTest.SubProperty)];
            Assert.IsType<SchemaObjectProperty>(subProperty);
            SchemaObjectProperty objectProperty = (SchemaObjectProperty) subProperty;
            Assert.IsType<SchemaObject>(objectProperty.Element);
        }

        [Fact]
        public void ConvertSubTable()
        {
            ISchemaElement element = SchemaFactory.Convert(typeof(SchemaClassTest));
            Assert.IsType<SchemaObject>(element);
            SchemaObject schema = (SchemaObject)element;

            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaClassTest.SubProperty));
            Assert.Contains(schema.Properties, prop => prop.Name == nameof(SchemaClassTest.TableList));

            ISchemaObjectProperty? subProperty = schema[nameof(SchemaClassTest.TableList)];
            Assert.IsType<SchemaObjectProperty>(subProperty);
            SchemaObjectProperty objectProperty = (SchemaObjectProperty)subProperty;
            Assert.IsType<SchemaTable>(objectProperty.Element);
        }
    }
}
