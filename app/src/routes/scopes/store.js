import { writable } from 'svelte/store';
import { API } from "$lib/store";

export class Scope
{
    /** @param {string} _type */
    constructor(_type = "")
    {
        /** @type {string|undefined} */
        this._id = undefined;
        this.name = "";
        this.parentId = "";

        /** @type {string} */
        this.type = _type;
        /** @type {string|undefined} */
        this.actionId = undefined;
        /** @type {Array<any>|undefined} */
        this.children = undefined;
    }
}

/** @param {string} scopeId */
export async function loadCurrent(scopeId)
{
    const reponse = await fetch(`${API.url}/scopes/${scopeId}`);
    const scope = await reponse.json();
    currentScope.set(scope);

    if (!scope._id) return;
    const reponseTree = await fetch(`${API.url}/scopes/${scopeId}/tree`);
    currentScopeTree.set(await reponseTree.json());
}

/** @param {Scope} _scope */
export async function updateCurrentScope(_scope)
{
    const response = await fetch(`${API.url}/scopes`, {
        method: "PUT", // *GET, POST, PUT, DELETE, etc.
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(_scope),
    });

    const updated = await response.json();
    currentScope.set(updated);
}

/** @param {Scope} _child */
export async function createScope(_child)
{
    const response = await fetch(`${API.url}/scopes`, {
        method: "POST", // *GET, POST, PUT, DELETE, etc.
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(_child),
    });
    loadCurrent(_child.parentId || 'root');
}

/** @param {Scope} _scope */
export async function createAction(_scope)
{
    const response = await fetch(`${API.url}/scopes/${_scope._id}/action`, {
        method: "POST", // *GET, POST, PUT, DELETE, etc.
        headers: {
            "Content-Type": "application/json",
        },
        body: "{}"
    });
    return await response.json();
}

export const currentScope = writable(new Scope());
export const currentScopeTree = writable([]);