<script>
    let { scopes = [] } = $props();
    let selectedScope = scopes[0];
</script>

<div class="list-group">
    {#each scopes as scope}
        <a
            href="#"
            class="list-group-item list-group-item-action"
            class:active={scope.id == selectedScope.id}
            on:click={() => selectedScope = scope}>{scope.name}</a
        >
    {/each}
</div>
