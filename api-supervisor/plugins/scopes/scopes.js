import { db } from '#lib/database.js';
import { ObjectId } from 'mongodb';

const scopes = db.collection("scopes");

async function getRoot(req, reply) {
    const rootScopes = await scopes.find({
        parentId: null
    }).sort({ type: 1, name: 1 }).toArray();

    return reply.status(200).send({
        id: null,
        name: 'root',
        children: rootScopes || [],
    });
}

async function createScope(req, reply) {

    req.body.parentId = req.body.parentId ? new ObjectId(req.body.parentId) : null;
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