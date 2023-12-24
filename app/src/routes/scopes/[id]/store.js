import { writable } from 'svelte/store';

export class Scope
{
    constructor()
    {
        this._id = "";
        this.name = "";
        this.children = [];
    }
}

export const currentScope = writable(new Scope());
export const currentScopeTree = writable([]);