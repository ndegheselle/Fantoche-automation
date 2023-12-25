<script>
    import { page } from "$app/stores";
    import { API } from "$lib/store";
    import { currentScope, currentScopeTree, Scope } from "./store";
    import ScopeModal from "../ScopeModal.svelte";

    $: loadScope($page.params.id);

    /** @param {string} scopeId */
    async function loadScope(scopeId) {
        const reponse = await fetch(`${API.url}/scopes/${scopeId}`);
        $currentScope = await reponse.json();

        if (!$currentScope._id) return;
        const reponseTree = await fetch(`${API.url}/scopes/${scopeId}/tree`);
        $currentScopeTree = await reponseTree.json();
    }

    /** @type {any} */
    let scopeModal = null;
    /** @type {any} */
    let actionModal = null;
</script>

<div class="columns is-gapless mb-0">
    <div class="column side-menu">
        <aside class="menu m-2">
            <div class="field has-addons">
                <div class="control has-icons-right is-fullwidth">
                    <input
                        class="input is-small"
                        type="email"
                        placeholder="Search"
                    />
                    <span class="icon is-small is-right">
                        <i class="fa-solid fa-magnifying-glass"></i>
                    </span>
                </div>
                <div class="control">
                    <div class="dropdown">
                        <div class="dropdown-trigger">
                            <button
                                class="button is-small is-success"
                                aria-haspopup="true"
                                aria-controls="dropdown-menu"
                            >
                                <i class="fa-solid fa-plus"></i>
                            </button>
                        </div>
                        <div
                            class="dropdown-menu"
                            id="dropdown-menu"
                            role="menu"
                        >
                            <div class="dropdown-content">
                                <a
                                    href="#"
                                    class="dropdown-item"
                                    on:click={() =>
                                        scopeModal.show(new Scope())}
                                >
                                    <i class="fa-solid fa-folder"></i> Add scope
                                </a>
                                <a
                                    href="#"
                                    class="dropdown-item"
                                    on:click={() =>
                                        scopeModal.show(new Scope("action"))}
                                >
                                    <i class="fa-solid fa-gears"></i> Add action
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {#if $currentScope.children}
                <ul class="menu-list">
                    {#each $currentScope.children as scope}
                        <li>
                            <a href="/scopes/{scope._id}">
                                <i
                                    class="fa-solid {scope.type == 'action'
                                        ? 'fa-gears'
                                        : 'fa-folder'}"
                                ></i>
                                {scope.name}
                            </a>
                        </li>
                    {/each}
                </ul>
            {/if}
        </aside>
    </div>
    <div class="column">
        {#if $currentScope._id}
            <nav class="breadcrumb" aria-label="breadcrumbs">
                <ul>
                    <li><a href="/scopes/root">..</a></li>
                    {#each $currentScopeTree as scope}
                        <li>
                            <a href="/scopes/{scope._id}">{scope.name}</a>
                        </li>
                    {/each}

                    <li class="is-active">
                        <a aria-current="page">{$currentScope.name}</a>
                    </li>
                </ul>
            </nav>
        {/if}
        <main class="container is-fluid">
            <p>Some content</p>
        </main>
    </div>
</div>

<ScopeModal bind:modal={scopeModal} />

<style global>
    .side-menu {
        max-width: 15rem;
        height: 100vh;
        box-sizing: border-box;
    }
</style>
