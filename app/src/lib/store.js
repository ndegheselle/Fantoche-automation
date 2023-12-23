import { writable, get } from 'svelte/store';

export const API = {
    url: "http://api-supervisor:3001"
};

export const alert = writable(null);

export const contextMenu = writable({
    visible: false,
    position: {x: 0, y: 0},
    items: [],
    context: null,
});