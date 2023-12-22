<script>
    import { currentScope } from "./store";
    import { API_URL } from "$lib/params";

    async function save() {
        const response = await fetch(`${API_URL}/scopes`, {
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
                _scope.childs.push(newScope);
                return _scope;
            },
        );

        scope = null;
    }

    /** @type {any} */
    let scope = null;
    export const modal = {
        show(_scope = null) {
            scope = _scope;
        },
    };
</script>

{#if scope}
    <div class="modal is-active">
        <div class="modal-background" on:click={() => (scope = null)}></div>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Add scope</p>
                <button
                    class="delete"
                    aria-label="close"
                    on:click={() => (scope = null)}
                ></button>
            </header>
            <section class="modal-card-body">
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
            </section>
            <footer class="modal-card-foot">
                <button class="button is-success" on:click={save}>Add</button>
                <button class="button" on:click={() => (scope = null)}
                    >Cancel</button
                >
            </footer>
        </div>
    </div>
{/if}
