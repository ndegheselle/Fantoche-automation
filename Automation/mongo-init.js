db.createUser(
    {
        user: process.env.MONGO_INITDB_ROOT_USERNAME, 
        pwd: process.env.MONGO_INITDB_ROOT_PASSWORD,
        roles: [
            {
                role: "readWrite",
                db: process.env.MONGO_INITDB_DATABASE
            }
        ]
    }
);

db.createCollection("task_instances");
db.createCollection("scopes");
db.createCollection("tasks");
db.createCollection("graphs");
db.createCollection("schedules");

db.scopes.createIndex(
    { parentTree: 1 },
    { background: true }
);
db.tasks.createIndex(
    { parentTree: 1 },
    { background: true }
);

// Root scope
db.scopes.insertOne({
    _id: UUID("00000000-0000-0000-0000-000000000001"),
    parentTree: [],
    name: "..",
    context: {}
});
