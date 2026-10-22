# Plan : packages served by the supervisor

**Goal** : the workers and the application (supervisor mode) download the packages from the
supervisor over HTTP, into their own cache. The shared folder no longer holds the packages.
Standalone mode keeps reading them from a local folder.

## Today

- The supervisor writes the `.nupkg` / `.snupkg` files into `SHARED_FOLDER/nuget`.
- The workers and the application read that same folder through `LocalPackageManagement`,
  then extract the assemblies into their own cache (`DATA_FOLDER/cache`, or the app's local folder).
- So every machine running a task must mount the shared folder, only to read packages the
  supervisor already holds.

## Target

```
Supervisor                         Worker / App (supervisor mode)
DATA_FOLDER/nuget/*.nupkg  ──HTTP──>  PackageCache ──> DATA_FOLDER/cache/{id}/{version}/*.dll
  GET /packages/{id}/{version}/file
  GET /packages/{id}/{version}/symbols

App (standalone)
./nuggets/*.nupkg  ──file──>  PackageCache ──> {local}/cache/{id}/{version}/*.dll
```

What a task runner needs is unchanged : `IPackageManagement.DownloadPackageAsync` returns
the path of a dll. Only where the `.nupkg` comes from changes.

## Steps

Each step is one commit and leaves the solution building with green tests.

### 1. Split `LocalPackageManagement` (no behavior change)

It does two jobs today : manage a folder of packages, and extract them into a cache.
Only the second one is needed by a worker.

- `PackageFolder` : the repository. `SearchAsync`, `GetInfosAsync`, `GetVersionsAsync`,
  `AddAsync`, `RemoveAsync`, plus `OpenPackageAsync(id, version)` and
  `OpenSymbolsAsync(id, version)` returning the file stream, or `null` when absent.
- `IPackageSource` : those two `Open*Async` methods. Implemented by `PackageFolder` now,
  by `SupervisorPackageSource` in step 3.
- `PackageCache : IPackageManagement` : built from an `IPackageSource` and a cache folder.
  Holds the framework selection, the extraction and the symbols (all moved as is).

All three live in `Fantoche.Worker/Packages`, next to `IPackageManagement`.
`LocalPackagesService` builds a `PackageFolder` and a `PackageCache` on top of it.

Two fixes go in the same step, because downloading over the network makes them more likely :

- **One extraction at a time per package.** `_downloadLocks` is declared but never used.
  Two executions of the same new package on one worker extract the same files at once.
  Take the per-package lock around fetch + extract.
- **No half-extracted package.** Any dll in the cache folder counts as "already cached".
  A failed extraction leaves a broken package forever. Extract into a temporary folder,
  then move it into place.

### 2. Supervisor : own the packages and serve them

- The packages folder moves from `SHARED_FOLDER/nuget` to `DATA_FOLDER/nuget` : the
  supervisor is the only one to read it now.
- Add to `PackagesApi` :
  - `GET /packages/{id}/{version}/file` : the `.nupkg`, 404 when unknown.
  - `GET /packages/{id}/{version}/symbols` : the `.snupkg`, 404 when there is none.
- The version is normalized the same way as the file names (`NuGetVersion.ToNormalizedString`).
- No authentication, like the rest of the API (see the readme).

### 3. Workers and app download from the supervisor

- `SupervisorPackageSource : IPackageSource` in `Fantoche.Worker/Packages` : an `HttpClient`
  on the supervisor address. A 404 is `null`, any other failure throws
  (`PackageDownloadException` through `PackageCache`).
- `Worker.cs` : `new PackageCache(new SupervisorPackageSource(supervisor), DATA_FOLDER/cache)`.
  `SHARED_FOLDER` is now only used for the storage.
- `SpineViewModel.CreateSupervisorServices` : same, with the app's local cache.
- `SpineViewModel.CreateStandaloneServices` : unchanged, `PackageFolder(Settings.PackagesFolderPath)`.

Symbols : `PackageCache` only asks for them when the host is a debug build
(`IsHostDebuggable`). A released worker never downloads them. A debug app does.

### 4. Configuration and docs

- `docker-compose.yml` : the worker keeps the shared volume for the storage only.
  A worker with no `DATA_FOLDER` volume re-downloads its packages after a restart : that's fine.
- `Settings.SharedFolder` and the settings page : "the storage shared with the workers".
- `readme.md` : packages are uploaded to and served by the supervisor ; the shared folder
  holds the storage only.
- Doc comments of `Worker`, `SupervisorApp`, `PackagesApi`, `RemotePackagesService`.

### 5. Tests

In `Fantoche.Worker.Tests`, against an in-process supervisor like `RemoteExecutionTests`.
The test package is built in the test with `NuGet.Packaging.PackageBuilder` from the
`Fantoche.Plugins.dll` already next to the tests, then uploaded through the API.

- `Download_ExtractsThePackageTheSupervisorHolds` : the returned dll exists in the cache.
- `Download_FailsForAPackageTheSupervisorDoesNotHold` : throws `PackageDownloadException`.
- `Download_ConcurrentRequestsForTheSamePackageAllSucceed` : reproduces the missing lock
  (step 1), written before the fix.

The existing execution tests keep `TestPackageManagement` : they test the execution, not
where the packages come from.

## Rejected alternatives

- **A standard NuGet V3 feed** (service index + flat container). `NuGet.Protocol` could
  read it directly, but it is more endpoints than we use, symbols still need a custom
  endpoint, and nobody else needs to browse the feed.
- **A separate NuGet server** (BaGetter...) : one more service to deploy and configure.
- **Pushing packages to the workers** : the supervisor would have to track what each
  worker holds. Pulling on demand is simpler and already how the cache works.

## Open questions

1. **Re-uploading the same id + version.** `AddAsync` replaces the file, but a worker
   that already extracted it keeps the old dlls (already true today). Proposal : reject it
   with a 409, as nuget.org does, and ask for a new version.
2. **Existing installs.** Packages already in `SHARED_FOLDER/nuget` must move to
   `DATA_FOLDER/nuget`. Proposal : a line in the readme, no migration code.
3. **Prerelease versions** (`1.0.0-beta`) are lost through `System.Version` everywhere.
   Out of scope here.
