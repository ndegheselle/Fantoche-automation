# Getting started

## Docker

Start :
```
docker-compose up
```

Build (after Dockerfile modif) :
```
docker-compose build
```

Clean docker :
```
docker-compose down -v --rmi all --remove-orphans
```

Execute a cmd in a container :
```
docker-compose exec [nom-container] /bin/bash
```

### Silence
Start with :
```
docker-compose up -d
```

Or simply only show the log of one container :
```
docker-compose logs api -f
```

## Database

### Setup a replica mongodb database

#### Windows

Tutorial : https://adelachao.medium.com/create-a-mongodb-replica-set-in-windows-edeab1c85894

#### Linux

Open the `mongosh` cmd on the db server :
```
docker-compose exec db /bin/sh
mongosh
```
Use this setup :
```js
rs.initiate()
```

### Prisma ORM
Push schema to database : 
```
npx prisma db push
```

Start prisma studio (interface to DB) :
```
npx prisma studio
```

### Default user

`test` password hashed :
```
$2b$10$DCmF8dlyil4vK5CUSzUgeO5Uoip1DRvTFaGeUXHgxElikdGbqguwu
```