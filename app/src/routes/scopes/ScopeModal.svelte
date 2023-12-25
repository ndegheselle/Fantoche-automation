<script>
    import { currentScope, Scope } from "./[id]/store";
    import { API } from "$lib/store";
    import popups from "$lib/dom/popups";

    async function save() {
        scope.parentId = $currentScope._id;
        const response = await fetch(`${API.url}/scopes`, {
            method: "POST", // *GET, POST, PUT, DELETE, etc.
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(scope), // body data type must match "Content-Type" header
        });

        const newScope = await response.json();
        currentScope.update(
            /** @param {any} _scope */
            (_scope) => {
                _scope.children.push(newScope);

                // Order by type and name
                _scope.children.sort(
                    /** 
                     * @param {Scope} a
                     * @param {Scope} b
                     * */
                    ( a, b) => {
                    if (a.type == b.type) {
                        return a.name.localeCompare(b.name);
                    }
                    return a.type.localeCompare(b.type);
                });

                return _scope;
            },
        );

        scope = null;
        popups.close(modalElement);
    }

    /** @type {any} */
    let scope = null;
    /** @type {Element|null}*/
    let modalElement = null;
    export const modal = {
        show(_scope = null) {
            scope = _scope;
            popups.show(modalElement);
        },
    };
</script>

<div class="modal" bind:this={modalElement}>
    <div class="modal-background"></div>
    <div class="modal-card">
        <header class="modal-card-head">
            <p class="modal-card-title">Add {scope?.type == "action" ? "action" : "scope"}</p>
            <button
                class="delete"
                aria-label="close"
                data-dismiss="modal"
            ></button>
        </header>
        <section class="modal-card-body">
            {#if scope}
                <div class="field">
                    <div class="control">
                        <input
                            class="input"
                            type="text"
                            placeholder="Name"
                            bind:value={scope.name}
                        />
                    </div>
                </div>
            {/if}
        </section>
        <footer class="modal-card-foot">
            <button class="button is-success" on:click={save}>Add</button>
            <button class="button" data-dismiss="modal"
                >Cancel</button
            >
        </footer>
    </div>
</div>
