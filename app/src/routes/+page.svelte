<script>
    import { onMount } from 'svelte';
    import Scope from './Scope.svelte';
    import ScopeModal from './ScopeModal.svelte';

    import {API_URL} from '$lib/params';
    import {scopes} from '$lib/globalStores';

    onMount(async () => {
        const reponse = await fetch(`${API_URL}/scopes/root`, {mode: "cors"});
        $scopes = await reponse.json();
	});

    /** @type {any} */
    let scopeModal = null;
</script>

<div class="columns is-gapless mb-0">
    <div class="column has-background-light side-menu">
        <aside class="menu has-background-light m-2">
            <div class="field has-addons">
                <p class="control has-icons-right">
                    <input
                        class="input is-small"
                        type="email"
                        placeholder="Search"
                    />
                    <span class="icon is-small is-right">
                        <i class="fa-solid fa-magnifying-glass"></i>
                    </span>
                </p>
                <div class="control">
                    <button
                        class="button is-small is-success"
                        on:click={() => scopeModal.show({name: ""})}
                    >
                        <i class="fa-solid fa-plus"></i>
                    </button>
                </div>
            </div>

            {#each $scopes as scope }
                <Scope {scope}/>
            {/each}

        </aside>
    </div>
    <div class="column">
        <main class="container is-fluid">
            <p>Content</p>
        </main>
    </div>
</div>

<ScopeModal bind:modal={scopeModal}/>

<style>
    .side-menu {
        max-width: 15rem;
        height: 100vh;
        box-sizing: border-box;
    }
</style>
