import { db } from '#lib/database.js';
import { ObjectId } from 'mongodb';

const actions = db.collection("actions");
const scopes = db.collection("scopes");

async function createAction(req, reply) {

    // Create related action
    const resultAction = await actions.insertOne({});
    const newScope = req.body;
    newScope.actionId = resultAction.insertedId;
    newScope.parentId = newScope.parentId ? new ObjectId(newScope.parentId) : null;

    const result = await scopes.insertOne(newScope);
    // Retrive the newly created scope
    const scope = await scopes.findOne({
        _id: result.insertedId
    });

    return reply.status(200).send(scope);
}

export default async function (app, opts) {
    app.post("/", createAction);
};