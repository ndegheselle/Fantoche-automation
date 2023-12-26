<script>
    import { currentScope, Scope, updateCurrentScope, createScope } from "./store";
    import popups from "$lib/dom/popups";

    async function save() {
        scope.parentId = $currentScope._id;

        if (scope._id) {
            await updateCurrentScope(scope);
        } else {
            await createScope(scope);
        }

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
            <p class="modal-card-title">{scope?._id ? 'Update' : 'Create'} scope</p>
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
            <button class="button is-success" on:click={save}>Save</button>
            <button class="button" data-dismiss="modal"
                >Cancel</button
            >
        </footer>
    </div>
</div>
