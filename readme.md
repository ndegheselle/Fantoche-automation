
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