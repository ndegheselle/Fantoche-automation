# Automation
No-code solution to create workflow automation based on a node editor.

Objectives:

- Create tasks based on various technical packages.
- Create workflows composed of tasks and sub-workflows connected to each other.
	- During the execution of a workflow, pass the result of a node to each connected node.
	- Flexible data handling while still supporting typed task results.
- Order tasks and workflows by scopes.
- Provide context on scope, tasks, and workflows to allow complex settings.

Backend Requirements:

- Workers to execute tasks that can be horizontally scaled.
- Fast real-time communication (as opposed to loops with delays).

## Architecture

![Architecture](images/architecture.png)

## Getting started

- [Setting up a mongodb with docker in visual studio](https://medium.com/@hugo_cesar45/asp-net-core-web-api-net-8-docker-mongodb-8fab9a54f72c)

Start the `docker-compose` startup project for the backend.
Start `Automation.App` for the client.

## Links

- [Adonis UI](https://benruehl.github.io/adonis-ui/) for styles
- [Fontawesome](https://fontawesome.com/) icons
- [PropertyChanged.Fody](https://www.nuget.org/packages/PropertyChanged.Fody) for boilerplate
- [Nodify](https://miroiu.github.io/nodify/) for the node editor
