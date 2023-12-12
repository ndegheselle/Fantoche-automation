class Node {
    constructor(_type, _id) {
        this.$type = _type;
        this.id = _id;
        this.name = _id;
    }
}

class Scope extends Node {
    constructor(_id) {
        super("scope", _id);
        this.childs = [];
    }
}

class Action extends Node {
    constructor(_id) {
        super("action", _id);
        this.inputs = [];
        this.outputs = [];
    }
}

class Workflow extends Action {
    constructor() {
        super("workflow", _id);
        // Graph of Action
    }
}

export async function GET({ params, request }) {
    return new Response(
        JSON.stringify([
            new Scope().childs = [
                new Action("action1"),
                new Workflow("workflow1"),
                new Scope("scope1")
            ]
        ])
    );
}