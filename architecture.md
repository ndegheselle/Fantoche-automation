# Architecture

The standalone and remote modes run the same code: one supervisor (`LocalSupervisor`) and one kind of worker (`LocalWorker`). A remote supervisor only adds an HTTP/SignalR layer between them, and a remote worker only adds a hub connection.

Two rules keep the interactions simple:
- What the application starts runs in the application, unless the user starts it on the workers. The workers run what the supervisor dispatches to them: the schedules, and the runs started on them from the application.
- Once a worker (the application's included) starts an execution, everything of it stays there. Every node of a workflow runs where the workflow runs, and its tables and files never leave the storage of its run. A suspended run is resumed by whoever can reach that storage (see [Suspended runs](#suspended-runs)).
- The one exception is a page asking the user: it always runs in an application, where the user is. A worker hands it to one through the supervisor (see [User interface tasks](#user-interface-tasks)).

## Standalone and remote

In standalone mode, the supervisor runs inside the application, and the application is its only worker.

```mermaid
flowchart TD
    subgraph App[Application]
        supervisor[LocalSupervisor] <-- calls in the process --> worker[LocalWorker]
    end
    supervisor --> db[(SQLite)]
    worker --> storage[Storage of the application]
```

In remote mode, the application reaches the supervisor through its API, and the supervisor dispatches to the remote workers.

```mermaid
flowchart TD
    subgraph App[Application]
        client[SupervisorClient] <--> worker[LocalWorker]
    end
    subgraph Docker
        api[Supervisor API] --> supervisor[LocalSupervisor]
        connection[RemoteWorkerConnection] --> remoteWorker[LocalWorker]
    end
    client <-- HTTP / SignalR --> api
    connection <-- HTTP / SignalR --> api
    supervisor --> db[(SQLite)]
    worker --> storage[Storage of the application]
    remoteWorker --> workerStorage[Storage of the worker]
```

| | Standalone | Remote |
| --- | --- | --- |
| Supervisor | `LocalSupervisor` in the application | `LocalSupervisor` behind `Fantoche.Supervisor.Api`, reached with `SupervisorClient` |
| Transport | Calls in the process, messages copied as over the wire | HTTP for the data, SignalR hubs for the executions and the history |
| Workers the supervisor dispatches to | The application | The worker services (`Fantoche.Worker.Service`) |
| A workflow started from the application | Runs in the application, or through the supervisor on the application's own worker ("On remote worker") | Runs in the application, or on the worker services ("On remote worker") |

Both supervisors implement `ISupervisor`, which is all the application and the workers know of them. That is why the mode only changes how the application is built (`SpineViewModel.AddServices`), never what it does.

In the process, the messages are copied the way the wire would copy them (`FantocheJson.Copy`). The caller and the worker never share an instance, so a bug can't hide in one mode only.

## An execution dispatched to a worker

An execution is handed whole to the supervisor, and the worker it is dispatched to runs all of it. A caller waiting on its outcome hands it over with `ICallerHub.Execute` (what `RemoteTaskExecutor` calls, for a page asking the user); a run nobody waits on but the history (a schedule, a workflow started "On remote worker") is started with `IDispatchService.StartAsync` (`POST /dispatch/tasks/{id}/start`, `Dispatcher.StartAsync`):

```mermaid
sequenceDiagram
    participant Caller
    participant Supervisor
    participant Worker

    Caller->>Supervisor: Execute(instance)
    Supervisor->>Supervisor: load the task, store the instance, queue it
    Supervisor->>Worker: Execute (least busy worker with room)
    Worker->>Worker: run it, every node here, in its own storage
    Worker-->>Supervisor: states and notifications (stored in the history)
    Worker->>Supervisor: Complete(instance)
    Supervisor->>Caller: Completed(instance)
```

- Only JSON travels: the parameters and the output. A table or file the output points at stays in the storage of its run.
- A worker lost while running an execution leaves it suspended. A worker that reconnects meanwhile can't report on it anymore: its states are dropped.
- A worker registers with its roles (`EnumWorkerRoles`): the worker services take runs, up to their capacity. The applications take the tasks asking the user, as many as come, since those wait on the user rather than on a slot. In standalone mode the application takes both.
- A workflow started from the editor is only sent to the workers when the user asks for it in its start settings ("On remote worker"). These settings only show for a workflow expecting parameters: one taking none starts right away, in the application.
- A run that can't be loaded (`Dispatcher.StartAsync`) is stored as a failed run: nobody waits on it to be told.

### Why a single node is never sent to a worker

Sending a single node of a workflow to a worker was implemented, then removed. Each worker only reads its own storage, so the tables and files a node needed had to travel with it, through a transit held by the supervisor:
- the caller put the items the parameters pointed at in the transit, the worker took them into its storage, and the output came back the same way;
- the items were found by their ids anywhere in the JSON, and kept their ids once copied, so the `TableRef` and `FileRef` stayed valid;
- each side emptied the transit as it took the items, and the supervisor cleared it when a caller disconnected or when it started.

It worked, but it took a transit store in the supervisor, an HTTP API to stream it, a copy of every table a node read or wrote, and the cleanup of what a lost execution left behind. Few workflows need a node of theirs elsewhere, and such a node can be made a workflow of its own.

A "Run on a worker" setting on the tasks and workflows was removed too. Where an execution runs belongs to what starts it, not to the task or workflow itself: a schedule always runs on the workers, and a run started by hand runs in the application, close to the editor and the local files, unless the user chooses otherwise as they start it.

## Following a run

- The history of a task, workflow or scope only lists the runs started on their own (`IHistoryService.GetByScopedAsync`, `ParentInstanceId` empty). A node of a workflow is read from its run.
- The detail of an execution shows what it ran with, its output (or the error it failed with) and the executions it ran. It only reads: a run of a workflow is acted upon from its editor ("Open execution").
- The editor draws a run on the graph of its workflow, as the history holds it, then as it goes on (`WorkflowRunViewModel`). From there its pages are answered, and a run of the application is paused, stopped and resumed (see [Suspended runs](#suspended-runs)). Double-clicking a node shows the detail of its execution, or opens its page when it waits for the user.

## Suspended runs

A run nothing runs anymore, waiting to be resumed, is `Suspended`. Its output tells why:
- **Paused by the user** from the editor: the nodes it runs finish, no other task starts (the controls still pass, so a run whose tasks are over reaches its end), and its nested runs pause with it. Nothing runs twice.
- **Cut short** by a crash or a lost worker: the nodes it was running run again once resumed.

It is resumed by hand from the editor, the run opened on the graph of its workflow ("Resume"), or stopped instead, which cancels it for good. The editor only pauses, stops and resumes the runs of an application (`IExecutionService`): the supervisor resumes its own (`IDispatchService.ResumeAsync`, `POST /dispatch/runs/{id}/resume`), which nothing in the application calls anymore since the detail of an execution lost its "Resume".

| A run started by | Is cut short when | Is resumed by |
| --- | --- | --- |
| The application | The copy of the application running it is gone. Any copy checks when it starts | Any copy of the application on the same machine (`TaskInstance.ApplicationId`, the machine name) |
| The supervisor (dispatched) | Its worker is lost, or the supervisor running it is gone (checked when a supervisor starts), or the application running one of its tasks asking the user is lost | The supervisor, on any worker with room |

Several copies of the application can run side by side on a machine:
- Each process running executions (a copy of the application, a supervisor) holds a lock file for as long as it lives (`RunnerSessions`), and marks its runs with its session (`TaskInstance.SessionId`). A run whose session holds no lock anymore was left behind.
- A run is taken atomically as it is resumed (`IHistoryService.ClaimRunAsync`): two copies resuming it at once can't both run it.
- In standalone mode, the copies share the database, but the history is only pushed live within a copy: another copy's runs show once the page is read again.

Resuming walks the run again from its history (`WorkflowExecutor`, with `WorkflowInstance.StoredInstances`):
- A node that was running runs again from its start, with the parameters it was given. Every task can run more than once after a crash, so a task with side effects has to bear it. A pause lets the nodes finish, so nothing runs twice.
- A completed node is kept. Its next nodes that were never reached run.
- A join still waiting keeps the branches that already arrived (`TaskInstance.PreviousIds` holds them), and waits for the others.
- A nested workflow resumes from its own history.
- A run that reached its end is only closed.

What the history stores for this:
- Each instance stores the ids of the instances it carries on from (`PreviousIds`) and the shared context it was given. Pass-through nodes and loops are walked again from these links.
- The run stores its global context, the version of the graph it ran (`TaskInstance.WorkflowVersion`) and the version of every workflow nested in it (`TaskInstance.NestedVersions`).
- A run resumes on these versions, whatever the workflows became since (see [Workflow versions](#workflow-versions)). The tasks are read as they are now: a resumed node runs the package its task points at today, the newest version for a task following the latest one (see [Packages and caches](#packages-and-caches)).

A remote run carries on in the storage of its first worker, so the workers share their storage (`STORAGE_FOLDER`).

The executions still live in memory only (`ExecutionService`, `Dispatcher` and `WorkerSession` keep a handle on each running one, to cancel it or send it back to its caller). Resuming doesn't need them: everything it reads is in the history.

## Workflow versions

Each save that changes the graph of a workflow adds a version (`AutomationWorkflow.Version`), and every instance of a workflow keeps the version it ran (`TaskInstance.WorkflowVersion`), a run also keeping the versions of its nested workflows. A run opened from the history is drawn on its own version, read only, and resumed on it. It isn't opened while the graph holds unsaved changes: they are saved or undone first.

The versions are stored as reverse diffs, so a past version costs only what changed:

| Table | Holds |
| --- | --- |
| `GraphNodes`, `GraphConnectors`, `GraphConnections` | The latest version, read as it is |
| `GraphVersions` | One row per version. The latest has no changes; every older one keeps what turns the version after it back into itself: the nodes that changed (stored whole, with their connectors, `null` for one that didn't exist) and the connections added or removed |

- A past version is rebuilt from the latest one, applying the changes of each version back down to it (`IScopedService.GetVersionAsync`). The most recent ones, the most opened, are the cheapest.
- A save leaving the graph as it is (only the name or the settings changed) adds no version.
- Moving a node is a change: the version stores that node again.
- The versions of a workflow go with it when it is removed. Nothing else removes them yet: pruning one would mean merging its changes into the version before.

## User interface tasks

A page asks the user for the parameters of a task, before the task runs. Use it for what a form can't describe, like picking the columns of a file or composing a document. The page is a task of its own, so a workflow without it (a schedule, an automated run) runs the task unattended.

- **In the tree:** a page hangs under the task it asks for ("Add a page to the task").
- **In a workflow:** it is a round node attached under its task, through the diamond under the task (`GraphUserInterface`, `BaseGraphTask.UserInterface`). It sits apart from the branches: nothing flows into it nor out of it. A task takes one page, a page reads and hands over the parameters of its task (`TasksGraph.CanConnect`).
- **When the task is reached:** its parameters are resolved, the page is asked with them, and the task runs with what the user answered, carrying on from the node before it (`WorkflowExecutor.RunUiAsync`). A resume asks again a page that was asking, and runs the task with the answer kept when the page was answered but the task never started.

```mermaid
sequenceDiagram
    participant Worker as Worker running the run
    participant Supervisor
    participant App as Application
    Worker->>Supervisor: Execute(page), as a caller
    Supervisor->>Supervisor: queue it, waiting for a user (AwaitingUser)
    App->>Supervisor: Take(page), its user answering it from its run
    Supervisor->>App: Execute(page), the application being a worker for the pages
    App->>App: DoAsync, until it asks (AwaitingUser)
    App->>App: the page, calling the methods of the page task
    App->>Supervisor: the storage of the run, read and written through its API
    App->>Supervisor: Complete(page), the answer as its output
    Supervisor->>Worker: Completed(page)
    Worker->>Worker: the task, with the answer as its parameters
```

A run started in the application skips the supervisor: the application runs the page itself.

Why a task of its own rather than a switch on the task (an `AskUser` input, as before): a workflow run unattended holds only the task, the page is seen as a step of the workflow, and the task runs on a worker while only the page needs an application.

- **Writing one:** derive from `UserInterfaceTask<TParameters>`, `TParameters` being the parameters of the task, and give the folder of its page (`PageFolder`, relative to the assembly). Its page methods call static methods of the task, so the page applies the rules of the task rather than a copy of them:

  ```csharp
  public class MapColumnsUi : UserInterfaceTask<MapColumnsParameters>
  {
      public override string PageFolder => "UI/MapColumns";

      [PageMethod]
      public string? Check(MapColumnsParameters parameters) => MapColumnsTask.Check(parameters);
  }
  ```

  See `MapColumnsUi` and `ExportPdfUi` in `Fantoche.Plugins.Example`.
- **The page:** it reads the parameters of the task as `window.fantoche.input` and answers them with `await window.fantoche.answer(value)`, rejected when the answer doesn't fit what the task expects. `window.fantoche.close()` closes it unanswered, and it can be opened again.
- **Calling the page task:** `await window.fantoche.call('Suggest', args)` calls a method of the running page task marked `[PageMethod]`. It takes at most one value, plus the `ITaskRuntime` and a `CancellationToken` if it asks for them. So the page reuses the rules of the task rather than writing them again in JavaScript: `MapColumnsTask.Check`, `ExportPdfTask.PreviewAsync` rendering the real PDF.
- **Packing it:** the folder is packed next to the assembly, the way `Fantoche.Plugins.Example.csproj` does it:

  ```xml
  <Content Include="UI\**\*" Exclude="UI\**\*.cs" CopyToOutputDirectory="PreserveNewest" Pack="true" PackagePath="lib\$(TargetFramework)\UI" />
  ```

  A package keeps the folders it has under `lib/<framework>` once extracted.
- **Where it runs:** always in an application (`UserTasks`).
  - A run of the application runs it there.
  - A worker hands it to the supervisor as any caller does (`RemoteTaskExecutor`). The supervisor never hands it out on its own: it waits in the queue, `AwaitingUser` in the history, and the branch of the run with it.
  - A user answering it from its run takes it (`IWorkerRegistration.TakeAsync`): the supervisor hands it to that application (`EnumWorkerRoles.UserTasks`, which every application registers in both modes), and its page opens once the task asks. A second application taking it is refused. No page shows up unasked.
  - A worker reaching no page at all (no supervisor) fails the page, and its task never runs.
- **While it asks:** the page is `AwaitingUser` and the branch of its task waits, the rest of the run going on.
  - The worker keeps the run in memory meanwhile, and counts it against its capacity.
  - For a run of the application, the editor following the run opens the page by itself. Otherwise the application tells the user, who opens the run on the graph of its workflow (from its history, or the detail of the execution) and double-clicks the node waiting.
  - The run shows `AwaitingUser` in the history while a node under it waits, nested runs included, and goes back to `Progressing` once none does (`LocalHistoryService.CarryAwaitingUserUpAsync`). Answering takes the task itself, not the nested runs holding it.
  - Only the application running the page can answer it. A second answer is refused.
- **When it goes wrong:**
  - An application lost while running the page leaves the node `Suspended`, and its run with it. Resuming the run asks again.
  - A run canceled stops the page and closes it.
- **Showing files and tables:** the page reads the ones its input points at, and the ones a page method hands back (`PageFiles`).

  ```js
  image.src = fantoche.fileUrl(fantoche.input.Scan);
  const { Columns, Total, Rows } = await fantoche.readTable(fantoche.input.Orders, { offset: 0, limit: 100 });
  ```

  - A page can't browse the rest of the storage.
  - Tables are read a slice at a time (`limit` up to 1000), so a large one doesn't load whole.
  - For a run of the workers, the application reaches the storage of the run through the supervisor (`RemoteStorage`), which reads the volume the workers share. The queries run there, only their results travel; the dates come back as dates.
- **What the page can reach:** only the files of its folder and the storage above, served under `https://page.fantoche/` with a content security policy allowing nothing from the network. No navigation elsewhere, no new window, no object of the application. Only JSON travels: the input, the calls and the answer.

## What the editor previews of a task

The editor walks the graph without running it (`GraphExecutionPreview`) : a task hands over the sample of its output schema, which knows nothing of the data. A task handing over part of its input says so on the property of its input (`OutputPreview`), so the nodes after it read what that property resolved to :

```csharp
public class MapColumnsParameters
{
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]        // the output is this table...
    public TableRef Source { get; set; } = new();

    [JsonSchemaExtensionData(OutputPreview.MappingSchemaKey, OutputPreview.Whole)] // ...with these names in its mapping
    public List<ColumnMapping> Columns { get; set; } = [];
}
```

- Without it, `$previous.Mapping.CustomerId` after the mapping is an error in the editor, and isn't offered.
- The task has to hand over what it declares : the preview trusts it, the run doesn't check it.
- The value is the place in the output : a property name, or `""` for the output itself. Values handed over to the same place merge, the mappings of the two tables of a merge for instance.

See [the table tasks](Fantoche/Fantoche.Plugins.Example/readme.md) for the mapping of the columns these previews serve.

## Schedules

- A task or workflow holds its schedules (`BaseAutomationTask.Schedules`): a standard cron (`minute hour day-of-month month day-of-week`, read in local time) and the parameters each run starts with, checked against its input schema.
- They're edited in its Schedules tab, each change saved right away. The Scheduler page lists them all and lays out their runs over the next 7 days, hour by hour, to see where they pile up.
- The supervisor starts them (`Scheduler`), checking every minute what came due and dispatching it to its workers like any dispatched run: it can be suspended, resumed on any worker, and nobody waits on it but the history. A scheduled workflow that can't be loaded is stored as a failed run.
  - The cron is read in the time zone of the supervisor: set `TZ` on its container, or the schedules start in UTC while the Scheduler page shows them in the time of the user.
  - What came due while no supervisor ran is skipped, not caught up.
  - Every supervisor sharing the database (copies of the application in standalone mode) checks the schedules; each time a schedule comes due is claimed in the database first (`ScheduledStarts`), so a single one starts it. The claims are cleaned up with the history.
  - In standalone mode the application is the supervisor, so its schedules only run while it is open, on its own worker.

## Storage and cleanup

Each execution has its own storage folder. The nodes of a workflow write into the folder of its run.

```text
storage/
  {taskId}/
    {created}-{instanceId}/
      tables/t_{id}.parquet
      files/f_{id}/{name}
```

- The remote workers share one storage folder (`STORAGE_FOLDER`, a volume mounted in every worker and in the supervisor). The application keeps its own.
- The executions are kept for a month. Once a day, the supervisor removes the older ones: the folder of each run in the storage it is given (`STORAGE_FOLDER`, or the storage of the application in standalone mode), then the runs themselves (their instances go with them).
- In remote mode the supervisor can't reach the storage of the application: when it starts, the application removes its folders whose run the history doesn't hold anymore. A folder whose name doesn't follow the pattern is left alone.
- The supervisor serves the storage it reaches under `storage/runs/{runId}` (`StorageApi`): what an application running a task asking the user reads and writes of a run of the workers.

A table or file named in the history therefore only exists in the storage that execution wrote to, and only for a month.

## Workers and pending executions

The supervisor keeps its executions in memory only, so it pushes what it dispatches rather than storing it (`ISupervisor.Dispatch`):
- The `Dispatcher` raises `StatusChanged` with the whole status whenever a worker comes or goes, or an execution is queued, handed out or over.
- In remote mode, the supervisor API pushes it on the `hubs/dispatch` hub, and `GET /dispatch/status` reads it. After a reconnection, the client reads it again: what was pushed meanwhile is lost.
- The Workers page reads the status once when shown, then follows the changes until hidden. It shows each worker with what it runs, and the executions waiting for room, in the order they will be handed out.
- The application is always listed first. Its executions started in the application run on its own worker without going through the supervisor, so the page reads them from the execution service (`IExecutionService.RunningChanged`). The application is also a worker of the supervisor, for the tasks asking the user (and for the runs in standalone mode): both are shown as one row.

## Packages and caches

The supervisor holds the packages. Each worker downloads a package the first time a task needs it, and extracts it in its own cache:

```mermaid
flowchart LR
    upload[Package added from the application] --> nuget[Supervisor: nuget]
    nuget --> check[Supervisor: cache, to list the tasks of a package]
    nuget -- package files, on first use --> appCache[Application worker: cache]
    nuget -- package files, on first use --> workerCache[Worker service: cache]
    appCache --> loaders[Assemblies loaded once per worker]
    workerCache --> loaders
```

- A task points at a package and a version, or at no version to follow the latest one (`PackageIdentifier.Version` null, "Latest" when the class is picked, and the default). The newest version is asked to the supervisor each time the task runs (`PackageCache.DownloadPackageAsync`, `IPackageSource.GetVersionsAsync`), never cached, so a version added since the last run is picked up without editing the task. The samples follow the latest version of the example plugins.
- A cache never refreshes a version it already extracted, so a version can't be added twice. Publish a changed package under a new version.
- The supervisor's cache only serves to check what a package holds when it is added. The tasks themselves always run from the cache of a worker, in both modes.

## Folders

```text
Supervisor (DATA_FOLDER, or %AppData%/Automation in standalone mode)
  fantoche.db       database: scopes, tasks, workflows, history
  nuget/            the packages added
  cache/            the packages extracted to check what they hold

Worker (DATA_FOLDER, or %AppData%/Automation/worker for the application)
  cache/            the packages extracted to run the tasks
  storage/          the storage of its executions
```
