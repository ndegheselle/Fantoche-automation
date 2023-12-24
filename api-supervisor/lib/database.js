import { MongoClient } from 'mongodb';
let db;
let client;

async function connect(url) {
    if (db) return db;
    client = new MongoClient(url);
    db = client.db();
}

process.on('SIGINT', cleanup);
process.on('SIGTERM', cleanup);
process.on('SIGUSR2', cleanup); // nodemon uses SIGUSR2

function cleanup() {
    if (client) {
        client.close();
        console.log('MongoDB connection closed.');
    }
    process.exit();
}

export { connect, db };