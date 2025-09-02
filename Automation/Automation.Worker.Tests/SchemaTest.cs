using System.Runtime.Serialization;

namespace Automation.Worker.Tests
{
    public class SchemaClassTest
    {
        public string StringMember { get; set; } = "";
        public bool BoolMember { get; set; }
        public int IntMember { get; set; }
        public DateTime DateTimeMember { get; set; }

        public List<string> StringList { get; set; } = [];
        public List<SchemaSubClassTest> TableList { get; set; } = [];

        [IgnoreDataMember]
        public bool IgnoredBool { get; set; }
    }

    public class SchemaSubClassTest
    {
        public string SubStringMember { get; set; } = "";
        public bool SubBoolMember { get; set; }
        public int SubIntMember { get; set; }
        public DateTime SubDateTimeMember { get; set; }
    }

    public class SchemaTest
    {
        [Fact]
        public void Test1()
        {

        }
    }
}
