import { db } from '#lib/database.js';
import { ObjectId } from 'mongodb';

const scopes = db.collection("scope");

async function getScopeTree(req, reply) {

    const tree = [];
    console.log(req.params);
    let scope = await scopes.findOne({
        _id: new ObjectId(req.params.id)
    });
    while(scope.parentId)
    {
        const parentScope = await scopes.findOne({
            parentId: scope.parentId
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
    }).toArray() || [];

    return reply.status(200).send(scope);
}

async function updateScope(req, reply) {
    return reply.status(500);
}

async function deleteScope(req, reply) {
    return 500;
}

export default async function (app, opts) {
    app.get("/tree", getScopeTree);
    app.get("/", getScope);
    app.put("/", updateScope);
    app.delete("/", deleteScope);
};