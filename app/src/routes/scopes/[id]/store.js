import { writable } from 'svelte/store';

export class Scope
{
    /** @param {string} _type */
    constructor(_type = "")
    {
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

export const currentScope = writable(new Scope());
export const currentScopeTree = writable([]);