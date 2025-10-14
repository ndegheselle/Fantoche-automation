using Automation.Models.Work;
using Automation.Shared.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Automation.Dal
{
    /// <summary>
    /// Convention to set Id
    /// </summary>
    public class GuidIdConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberName == nameof(IIdentifier.Id))
            {
                memberMap.SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
                memberMap.SetIdGenerator(new GuidGenerator());
            }
        }
    }

    public class DatabaseConnection
    {
        public IMongoDatabase Database { get; private set; }

        public DatabaseConnection(string connectionString, string databaseName)
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            var conventionPack = new ConventionPack
            {
                new CamelCaseElementNameConvention(), // Built-in camel case convention
                new GuidIdConvention()                // Our custom GUID ID convention
            };

            ConventionRegistry.Register("CustomConventions", conventionPack, t => true);
            RegisterClassMaps();
            MongoClient client = new MongoClient(connectionString);
            Database = client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Register the class members mapping (know sub types and unmaped members).
        /// </summary>
        public void RegisterClassMaps()
        {
            BsonClassMap.RegisterClassMap<ScopedElement>(
                cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(Scope));
                    cm.AddKnownType(typeof(AutomationTask));
                    cm.AddKnownType(typeof(AutomationControl));
                    cm.AddKnownType(typeof(AutomationWorkflow));
                    cm.UnmapMember(m => m.Parent);
                });

            BsonClassMap.RegisterClassMap<BaseAutomationTask>(
                cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(AutomationTask));
                    cm.AddKnownType(typeof(AutomationControl));
                    cm.AddKnownType(typeof(AutomationWorkflow));
                });
            BsonClassMap.RegisterClassMap<TaskConnector>(
                cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(m => m.Schema);
                });

            BsonClassMap.RegisterClassMap<AutomationControl>(
                cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(m => m.Type);
                });

            BsonClassMap.RegisterClassMap<Scope>(
                cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(m => m.Childrens);
                });

            BsonClassMap.RegisterClassMap<GraphNode>(
                cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(GraphTask));
                    cm.AddKnownType(typeof(GraphGroup));
                });
            BsonClassMap.RegisterClassMap<GraphTask>(
                cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(m => m.Name);
                });
            BsonClassMap.RegisterClassMap<GraphConnector>(
                cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(m => m.Parent);
                    cm.UnmapMember(m => m.IsConnected);
                });
            BsonClassMap.RegisterClassMap<GraphConnection>(
                cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(m => m.Source);
                    cm.UnmapMember(m => m.Target);
                });

            BsonClassMap.RegisterClassMap<TaskInstance>(
                cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(SubTaskInstance));
                });

            BsonClassMap.RegisterClassMap<TaskTarget>(
                cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.AddKnownType(typeof(ClassTarget));
                    cm.AddKnownType(typeof(PackageClassTarget));
                });
        }
    }
}
