use uuid::Uuid;

// Shame that you cant extend existing struct or use trait for that
trait Node {
    id: Uuid,
    name: String
}

struct Scope {
    id: Uuid,
    name: String
}
// For serialization
impl Scope {
    const TYPE: String = "scope";
}

struct Action {
    node: Node,

    inputs: Vec<String>,
    outputs: Vec<String>
}
// For serialization
impl Action {
    const TYPE: String = "action";
}

struct Workflow {
    node: Node,
    action: Action,
    // TODO : Graph of actions
}
// For serialization
impl Workflow {
    const TYPE: String = "workflow";
}