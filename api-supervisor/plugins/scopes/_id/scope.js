import { db } from '#lib/database.js';
import { ObjectId } from 'mongodb';

const scopes = db.collection("scopes");
const actions = db.collection("actions");

async function getScopeTree(req, reply) {

    const tree = [];
    let scope = await scopes.findOne({
        _id: new ObjectId(req.params.id)
    });
    while(scope.parentId)
    {
        const parentScope = await scopes.findOne({
            _id: scope.parentId
        });
        scope = parentScope;
        tree.push(scope);
    }
    return reply.status(200).send(tree);
}

async function getScope(req, reply) {
    const scope = await scopes.findOne({
        _id: new ObjectId(req.params.id)
    });
    scope.children = await scopes.find({
        parentId: new ObjectId(req.params.id)
    }).sort({ type: 1, name: 1 }).toArray() || [];

    return reply.status(200).send(scope);
}

async function updateScope(req, reply) {
    return reply.status(500);
}

async function deleteScope(req, reply) {
    return reply.status(500);
}

export default async function (app, opts) {
    app.get("/tree", getScopeTree);
    app.get("/", getScope);
    app.put("/", updateScope);
    app.delete("/", deleteScope);
};