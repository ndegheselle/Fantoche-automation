using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(ClassTarget), "class")]
    [JsonDerivedType(typeof(PackageClassTarget), "package")]
    [JsonDerivedType(typeof(Graph), "graph")]
    [BsonKnownTypes(typeof(ClassTarget))]
    [BsonKnownTypes(typeof(PackageClassTarget))]
    [BsonKnownTypes(typeof(Graph))]
    public abstract class TaskTarget : ITaskTarget
    { }

    public class ClassTarget : TaskTarget, IClassTarget
    {
        public ClassIdentifier Class { get; set; }
        public ClassTarget(ClassIdentifier targetClass)
        {
            Class = targetClass;
        }
    }

    public class PackageClassTarget : ClassTarget, IPackageClassTarget
    {
        public PackageIdentifier Identifier { get; set; }

        public PackageClassTarget() : base(new ClassIdentifier())
        {
            Identifier = new PackageIdentifier();
        }

        public PackageClassTarget(PackageIdentifier identifier, ClassIdentifier targetClass) : base(targetClass)
        {
            Identifier = identifier;
        }
    }
}