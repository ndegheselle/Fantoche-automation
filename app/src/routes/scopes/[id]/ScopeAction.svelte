<script>
    import { Scope, createAction, currentScope } from "../store";

    async function create()
    {
        console.log('create action');
        action = await createAction($currentScope);
        scope.actionId = action._id;
    }

    let action = null;
    /** @type {Scope} */
    export let scope = new Scope();
</script>

{#if scope.actionId}
    <p>Show action graph</p>
{:else}
    <div class="hero-body">
        <div class="container has-text-centered">
            <p class="title">No action</p>
            <p class="subtitle">
                <button class="button is-white" on:click={create}>
                    <span class="icon">
                        <i class="fa-solid fa-plus"></i>
                    </span>
                    <span>create</span>
                </button>
            </p>
        </div>
    </div>
{/if}
