<script>
    import { currentScope } from "./store";
    import ScopeModal from "../ScopeModal.svelte";

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
                                <a href="#" class="dropdown-item" on:click={() => scopeModal.show(null)}>
                                    Add scope
                                </a>
                                <a class="dropdown-item">
                                    Add action
                                </a>
                                <a href="#" class="dropdown-item is-active">
                                    Add workflow
                                </a>
                                <a href="#" class="dropdown-item">
                                    Other dropdown item
                                </a>
                                <hr class="dropdown-divider" />
                                <a href="#" class="dropdown-item">
                                    With a divider
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {#each $currentScope.children as scope}
                <a href="/scopes/{scope.id}">{scope.name}</a>
            {/each}
        </aside>
    </div>
    <div class="column">
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
