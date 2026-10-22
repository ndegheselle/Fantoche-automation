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

1. Start the `docker-compose` startup project (or `Fantoche.Supervisor.Api` and `Fantoche.Worker.Service`).
2. Start `Fantoche.App`, and set the supervisor address and the shared folder in its settings (gear of the title bar).

### Modes

- **Supervisor** (default) : the supervisor holds the database, the application and the workers reach it through its API. A task or a workflow set to "Run on a worker" is sent to a worker, the rest runs in the application. A workflow is handed whole to a single worker.
- **Standalone** : the application keeps its own database, packages and storage, and has no worker (a task set to run on one fails). Only for debugging or very particular uses.

The mode, the supervisor and the shared folder are applied when the application starts.

### Configuration

- Supervisor : `DATA_FOLDER` (its database, the cache of the packages) and `SHARED_FOLDER`.
- Worker : `SUPERVISOR_URL`, `WORKER_ID`, `WORKER_CAPACITY` (executions run at once), `DATA_FOLDER` (the cache of the packages) and `SHARED_FOLDER`.
- The shared folder holds the `nuget` packages and the `storage` (tables and files). The supervisor, the workers and the application all reach it : everything flows between tasks through their parameters or the storage. Tasks writing the same data at once is for the workflow to avoid.
- The storage keeps what a run saved under `{workflow or task id}/{instance id}`, the nodes of a workflow (nested workflows included) under the run of the workflow. The Storage page of the application shows what each workflow or task takes.

The supervisor has no authentication yet.

## Links

- [Adonis UI](https://benruehl.github.io/adonis-ui/) for styles
- [Fontawesome](https://fontawesome.com/) icons
- [PropertyChanged.Fody](https://www.nuget.org/packages/PropertyChanged.Fody) for boilerplate
- [Nodify](https://miroiu.github.io/nodify/) for the node editor
- [PDFsharp](https://www.pdfsharp.com/) for the PDF task, used under the AGPL (set by the task, PDFsharp refusing to render without a license)

# TODO

- Scripting language to handle contexte change in tasks settings
- Generic way to create UI form for settings
    - Also handle validation and types with this ?
- Handle supervisor edge case for flow control tasks
- Add user management with roles and permissions
- Handle proper state (going back and forth with history) in the editor
- handle read and write tasks in the interface
- Supervisor

- Allow supervising the workers from their page (it only lists them and their queue)
- Workflow validation with potential errors prompting (infinite loop. dead branches, ...)

# ToThink

- Allow multiple supervisor and more complex worker assignation
- Re-dispatch the executions of a lost worker rather than failing them
- Resume a workflow after a crash (would need the previous instance and the shared context to be stored)
- display potential errors in workflow creation