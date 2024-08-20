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
db.createCollection("TaskHistories");
db.createCollection("Scopes");
db.createCollection("Tasks");

// Root scope
db.scopes.insertOne({
    _id: UUID("00000000-0000-0000-0000-000000000001"),
    parentId: null,
    name: "Root",
    context: {}
});