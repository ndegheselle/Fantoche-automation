# Fantoche
No-code solution to create workflow automation based on a node editor.

Objectives:

- Create tasks based on various technical packages.
- Create workflows composed of tasks and sub-workflows connected to each other.
	- During the execution of a workflow, pass the result of a node to each connected node.
	- Flexible data handling while still supporting typed task results.
- Order tasks and workflows by scopes.
- Provide context on scope, tasks, and workflows to allow complex settings.

Backend Requirements:

- One supervisor that handles comms between clients and workers. Supervisor is an unique entry point for simplification sake.
- Workers that can be horizontally scaled to execute tasks.
- Fast real-time communication between workers and supervisor (as opposed to loops with delays).

## Architecture

![Architecture](images/architecture.png)

## Getting started

Start `Fantoche.App` for the client : every task runs in the application by default.

### Remote execution

A task or a workflow set to "Run on a worker" (in its settings) is sent to a worker through the supervisor, the rest keeps running in the application. A remote workflow runs whole on a single worker.

- Start the `docker-compose` startup project (or `Fantoche.Supervisor.Api` and `Fantoche.Worker.Service`).
- Set `FANTOCHE_SUPERVISOR_URL` (e.g. `http://localhost:8080/`) before starting `Fantoche.App`. Without it, a task set to run on a worker fails.
- A worker is configured with `SUPERVISOR_URL`, `WORKER_ID`, `WORKER_CAPACITY` (executions run at once) and `DATA_FOLDER` (holding the `nuget` packages folder, their cache and the storage).

Current limits :

- The workers load the packages from their own `nuget` folder : it has to hold the packages of the application (shared volume).
- The storage (tables and files) is per machine : a table created on a worker can't be read by a task running in the application, and the other way around.
- The supervisor has no authentication.

## Links

- [Adonis UI](https://benruehl.github.io/adonis-ui/) for styles
- [Fontawesome](https://fontawesome.com/) icons
- [PropertyChanged.Fody](https://www.nuget.org/packages/PropertyChanged.Fody) for boilerplate
- [Nodify](https://miroiu.github.io/nodify/) for the node editor

# TODO

- Scripting language to handle contexte change in tasks settings
- Generic way to create UI form for settings
    - Also handle validation and types with this ?
- Handle supervisor edge case for flow control tasks
- Add user management with roles and permissions
- Handle proper state (going back and forth with history) in the editor
- handle read and write tasks in the interface
- Supervisor

- Display workers list and allow supervision
- Workflow validation with potential errors prompting (infinite loop. dead branches, ...)

# ToThink

- Allow multiple supervisor and more complex worker assignation
- Serve the packages from the supervisor (a nuget feed) rather than a folder shared with the workers
- Share the storage between the application and the workers
- Re-dispatch the executions of a lost worker rather than failing them
- display potential errors in workflow creation