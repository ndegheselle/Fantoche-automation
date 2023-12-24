import { db } from '#lib/database.js';
const scopes = db.collection("scope");

async function getRoot(req, reply) {
    const rootScopes = await scopes.find({
        parentId: null
    }).toArray();
    return reply.status(200).send({
        id: null,
        name: 'root',
        children: rootScopes
    });
}

async function createScope(req, reply) {
    const result = await scopes.insertOne(req.body);
    // Retrive the newly created scope
    const scope = await scopes.findOne({
        _id: result.insertedId
    });

    return reply.status(200).send(scope);
}

export default async function (app, opts) {
    app.get("/root", getRoot);
    app.post("/", createScope);
};