import { API_URL } from "$lib/params";
import { currentScope } from "../store";

/** @param {any} params */
export async function load({ fetch, params }) {
    const reponse = await fetch(`${API_URL}/scopes/${params.id}`, { mode: "cors" });
    currentScope.set(await reponse.json());
}