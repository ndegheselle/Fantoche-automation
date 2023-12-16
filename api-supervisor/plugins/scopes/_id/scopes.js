async function getScope(req, reply) {
    return reply.status(200).send(req.params);
}

async function updateScope(req, reply) {
    return reply.status(200).send(categories);
}

async function createScope(req, reply) {
    return reply.status(200).send(categories);
}

async function deleteScope(req, reply) {
    return reply.status(200).send(categories);
}

export default async function (app, opts) {
    app.get("/", getScope);
    app.put("/", updateScope);
    app.post("/", createScope);
    app.delete("/", deleteScope);
};