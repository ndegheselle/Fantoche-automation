import { writable } from 'svelte/store';

class Scope
{
    constructor()
    {
        this.id = 0;
        this.name = "";
        this.children = [];
    }
}

export const currentScope = writable(new Scope());