# Apis

- Handle auth with JWT and refresh tokens
- Use openapi to allow clients

Supervisor :
- Select worker based on available, queue size and other worker parameters
- Directly connect the client to the worker for progression 

Worker :
- Register itself on startup
    - Safe guard with mac or some unique hardware ID
    - Parameters : number of parallel tasks, max queue size, ...
    - State (updated) : available, working, down, updating ...
- Update current task instance state (success, warning, failure)
- Show actual / history queue with state
- List of "allowed"  and last instance state
- Can resolve a task based on it's type and select the correct way to execute it

Worker start