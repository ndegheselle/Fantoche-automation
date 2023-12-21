import { PrismaClient } from '@prisma/client';
const prisma = new PrismaClient();

async function getScope(req, reply) {
    return reply.status(200).send(await prisma.scope.findUnique({
        where: { id: req.params.id }
    }));
}

async function updateScope(req, reply) {
    return reply.status(200).send(await prisma.scope.update({
        where: {
            id: req.params.id,
        },
        data: req.body,
    }));
}

async function deleteScope(req, reply) {
    await prisma.scope.delete({
        where: {
            id: req.params.id,
        },
    });
    return reply.status(200).send({ message: "Scope deleted."});
}

export default async function (app, opts) {
    app.get("/", getScope);
    app.put("/", updateScope);
    app.delete("/", deleteScope);
};