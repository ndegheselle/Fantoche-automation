# Server

- Handle auth with JWT and refresh tokens
- Use openapi for the supervisor document
- Communicate with workers (supervisor, client) with a redis database

Supervisor (api) :
- Select worker based on available, queue size and other worker parameters
- Connect to the redis server to get the available workers

Worker (windows service) :
- Register itself on startup
    - Safe guard with mac or some unique hardware ID
    - Parameters : number of parallel tasks, max queue size, ...
    - State (updated) : available, working, down, updating ...
- Update current task instance state (success, warning, failure)
- Show actual / history queue with state
- List of "allowed"  and last instance state
- Can resolve a task based on it's type and select the correct way to execute it
- Keep realtime data (data that make sense only if the worker is up) and all the rest in the mongodb database

# Data contract and separation of concern

- Having coherant data definition between the database, api and app
- Keep a clear separation of concern but keeping the properties that should be shared, can be achieve with :
    - Interfaces : having extra properties for the properties that need a specific implementation
        - Viable on big classes with a lot of properties ?
    - DTO : using clonning / properties
        - Not a fan since it double everything + no clear way to ensure the DTO is compatible with the model
- Better serparation between ViewModel and Model, some other solution than using wrapper ?

Add common nameof for serialization attributes.