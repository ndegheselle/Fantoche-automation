db.createUser(
    {
        user: process.env.MONGO_INITDB_ROOT_USERNAME, 
        pwd: process.env.MONGO_INITDB_ROOT_PASSWORD,
        roles: [
            {
                role: "readWrite",
                db: "UserDB"
            }
        ]
    }
);
db.createCollection("TaskHistories");
db.createCollection("Scopes");
db.createCollection("Tasks");
db.createCollection("Workflows");