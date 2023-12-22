import { PrismaClient } from '@prisma/client';
const prisma = new PrismaClient();

async function getRoot(req, reply) {
    return reply.status(200).send({
        id: null,
        name: 'root',
        children: await prisma.scope.findMany({
            where: { parent: null }
        })
    });
}


async function createScope(req, reply) {
    return reply.status(200).send(await prisma.scope.create({
        data: req.body
    }));
}

export default async function (app, opts) {
    app.get("/root", getRoot);
    app.post("/", createScope);
};