import { db } from '#lib/database.js';
import { ObjectId } from 'mongodb';

const actions = db.collection("actions");
const scopes = db.collection("scopes");

async function createAction(req, reply) {

    // Create related action
    const result = await actions.insertOne({
        scopeId: new ObjectId(req.params.id),
    });

    // update scope with actionId
    await scopes.updateOne({
        _id: new ObjectId(req.params.id)
    }, {
        $set: {
            actionId: result.insertedId
        }
    });

    const action = await actions.findOne({
        _id: result.insertedId
    });

    return reply.status(200).send(action);
}

export default async function (app, opts) {
    app.post("/", createAction);
};