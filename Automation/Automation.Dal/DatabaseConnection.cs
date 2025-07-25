using Automation.Dal.Models;
using Automation.Shared.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace Automation.Dal
{
    /// <summary>
    /// Convention to set Id
    /// </summary>
    public class IdGeneratorConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberName == nameof(IIdentifier.Id))
            {
                memberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
                memberMap.SetSerializer(BsonSerializer.LookupSerializer(typeof(Guid)));
                memberMap.SetIsRequired(true);
            }
        }
    }

    public class DatabaseConnection
    {
        public IMongoDatabase Database { get; private set; }
        public DatabaseConnection(IMongoDatabase _mongo)
        {
            Database = _mongo;
            // Apply convention globaly
            ConventionRegistry.Register(nameof(IdGeneratorConvention), new ConventionPack { new IdGeneratorConvention() }, t => true);
            RegisterClassMaps();
        }

        /// <summary>
        /// Register the class members mapping (know sub types and unmaped members).
        /// </summary>
        public void RegisterClassMaps()
        {
            BsonClassMap.RegisterClassMap<ScopedElement>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(Scope));
                cm.AddKnownType(typeof(AutomationTask));
                cm.AddKnownType(typeof(AutomationControl));
                cm.AddKnownType(typeof(AutomationWorkflow));
                cm.UnmapMember(m => m.Parent);
            });

            BsonClassMap.RegisterClassMap<BaseAutomationTask>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(AutomationTask));
                cm.AddKnownType(typeof(AutomationControl));
                cm.AddKnownType(typeof(AutomationWorkflow));
            });

            BsonClassMap.RegisterClassMap<AutomationControl>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.Type);
            });

            BsonClassMap.RegisterClassMap<Scope>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.Childrens);
            });

            BsonClassMap.RegisterClassMap<GraphNode>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(GraphTask));
                cm.AddKnownType(typeof(GraphGroup));
            });
            BsonClassMap.RegisterClassMap<GraphTask>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.Name);
            }); 
            BsonClassMap.RegisterClassMap<GraphConnector>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.Parent);
                cm.UnmapMember(m => m.IsConnected);
            });
            BsonClassMap.RegisterClassMap<GraphConnection>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(m => m.Source);
                cm.UnmapMember(m => m.Target);
            });

            BsonClassMap.RegisterClassMap<TaskInstance>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(SubTaskInstance));
            });

            BsonClassMap.RegisterClassMap<TaskTarget>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.AddKnownType(typeof(ClassTarget));
                cm.AddKnownType(typeof(PackageClassTarget));
            });
        }
    }
}
