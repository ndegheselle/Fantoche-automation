import { API } from "$lib/store";
import { currentScope } from "./store";

/** @param {any} params */
export async function load({ fetch, params }) {
    const reponse = await fetch(`${API.url}/scopes/${params.id}`, { mode: "cors" });
    currentScope.set(await reponse.json());
}