import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

import fastify from 'fastify';
import cors from '@fastify/cors';
import multipart from '@fastify/multipart';
import autoLoad from '@fastify/autoload';
import { connect } from './lib/database.js';

import * as dotenv from 'dotenv';

dotenv.config();
connect(process.env.DATABASE_URL);

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

const app = fastify({
  logger: true
})

app.register(cors, {
  origin: true,
  methods: ['GET', 'PUT', 'POST', 'DELETE'],
  credentials: true
});
app.register(multipart);

app.register(autoLoad, {
  dir: join(__dirname, 'plugins'),
  autoHooks: true,
  cascadeHooks: true,
  routeParams: true
  // options: { prefix: 'api' }
})

app.listen({host: '0.0.0.0', port: process.env.PORT })