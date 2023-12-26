<script>
    import { page } from "$app/stores";
    import {
        currentScope,
        currentScopeTree,
        Scope,
        loadCurrent,
    } from "../store";
    import ScopeModal from "../ScopeModal.svelte";
    import ScopeHistory from "./ScopeHistory.svelte";
    import ScopeAction from "./ScopeAction.svelte";

    $: {
        loadCurrent($page.params.id);
        currentView = "history";
    }

    /** @type {any} */
    let scopeModal = null;
    let currentView = "history";
</script>

<div class="columns is-gapless mb-0">
    {#if $currentScope.type != 'action'}
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
    {/if}
    <div class="column">
        <nav class="navbar" role="navigation" aria-label="main navigation">
            <div id="navbarBasicExample" class="navbar-menu">
                <div class="navbar-start">
                    <div class="navbar-item">
                        {#if $currentScope._id}
                            <nav class="breadcrumb" aria-label="breadcrumbs">
                                <ul>
                                    <li><a href="/scopes/root">..</a></li>
                                    {#each $currentScopeTree as scope}
                                        <li>
                                            <a href="/scopes/{scope._id}"
                                                >{scope.name}</a
                                            >
                                        </li>
                                    {/each}

                                    <li class="is-active">
                                        <p>{$currentScope.name}</p>
                                    </li>
                                </ul>
                            </nav>
                        {/if}
                    </div>
                </div>
                {#if $currentScope._id != null}
                <div class="navbar-end">
                    <div class="navbar-item">
                        <div class="buttons">
                            <button class="button is-small is-light" on:click={() => scopeModal.show($currentScope)}>
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="button is-small is-danger">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
                {/if}
            </div>
        </nav>

        <main class="container is-fluid">
            {#if $currentScope.type == 'action'}
            <div class="tabs">
                <ul>
                    <li class:is-active={currentView == 'history'}><a on:click={() => currentView = 'history'}>History</a></li>
                    <li class:is-active={currentView == 'action'}><a on:click={() => currentView = 'action'}>Action</a></li>
                </ul>
            </div>
            {/if}

            {#if currentView == 'history'}
            <ScopeHistory scope={$currentScope}/>
            {:else if currentView == 'action'}
            <ScopeAction scope={$currentScope}/>
            {/if}
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
