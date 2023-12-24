import { db } from '#lib/database.js';

const scopes = db.collection("scopes");

async function getScope(req, reply) {
    const scope = await scopes.findOne({
        _id: req.params.id
    });
    scope.children = await scopes.find({
        parentId: req.params.id
    });

    return reply.status(200).send(scope);
}

async function updateScope(req, reply) {
    return reply.status(500);
}

async function deleteScope(req, reply) {
    return 500;
}

export default async function (app, opts) {
    app.get("/", getScope);
    app.put("/", updateScope);
    app.delete("/", deleteScope);
};