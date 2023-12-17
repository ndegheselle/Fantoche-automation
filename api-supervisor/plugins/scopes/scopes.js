import { PrismaClient } from '@prisma/client';
const prisma = new PrismaClient();

async function createScope(req, reply) {
    return reply.status(200).send(await prisma.account.create({
        data: req.body
    }));
}

export default async function (app, opts) {
    app.post("/", createScope);
};