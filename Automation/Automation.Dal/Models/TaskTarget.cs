using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(ClassTarget), "class")]
    [JsonDerivedType(typeof(PackageClassTarget), "package")]
    [BsonKnownTypes(typeof(ClassTarget))]
    [BsonKnownTypes(typeof(PackageClassTarget))]
    public abstract class TaskTarget : ITaskTarget
    { }
}