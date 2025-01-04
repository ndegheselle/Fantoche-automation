# Getting started

[Setting up a mongodb with docker in visual studio](https://medium.com/@hugo_cesar45/asp-net-core-web-api-net-8-docker-mongodb-8fab9a54f72c)

Todo :
- Create a client specific redis user for the app 
- Display task progression (state changes) and progression messages
- Fix workflow creation and save
    - Select task with no versions (last by default) or with specific version
- Setup workflow execution (on the worker service)
- Display workflow progression with each task state
    - Send progression from tasks
    - Display individual tasks on the workflow
- Save scheduled tasks and execute them
