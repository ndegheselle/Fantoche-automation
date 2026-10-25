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

See [architecture.md](architecture.md) : the standalone and remote modes, where an execution runs and its storage, the packages and their caches.

## Getting started

1. Start the `docker-compose` startup project (or `Fantoche.Supervisor.Api` and `Fantoche.Worker.Service`).
2. Start `Fantoche.App`, and set the supervisor address in its settings (gear of the title bar).
3. To see the table tasks at work, run the "Customers and orders" sample : see [the table tasks](Fantoche/Fantoche.Plugins/readme.md).

### Modes

- **Supervisor** (default) : the supervisor holds the database, the application and the workers reach it through its API. What the application starts runs in the worker of the application, the worker services run what the supervisor dispatches (the scheduled items, to come). A workflow is handed whole to a single worker, and its nodes all run where it runs.
- **Standalone** : the supervisor runs in the application, which is its only worker. Only for debugging or very particular uses.

The mode and the supervisor are applied when the application starts.

### Configuration

- Supervisor : `DATA_FOLDER` (its database, the `nuget` packages, their cache and the storage in `transit`).
- Worker : `SUPERVISOR_URL`, `WORKER_ID`, `WORKER_CAPACITY` (executions run at once) and `DATA_FOLDER` (the `cache` of the packages and the `storage` of its executions).
- Application : its local folder (`%AppData%/Automation`) holds the data of its worker (`worker`) and, in standalone mode, the one of its supervisor (as in its `DATA_FOLDER`).
- A package version can't be added twice, since the caches would keep the first one : publish a changed package under a new version.
- Only the last executions of a task or a workflow keep their storage, on each worker : 10 by default, set on the task or the workflow ("Executions keeping their storage").

The supervisor has no authentication yet.

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
- Re-dispatch the executions of a lost worker rather than failing them
- Resume a workflow after a crash (would need the previous instance and the shared context to be stored)
- display potential errors in workflow creation