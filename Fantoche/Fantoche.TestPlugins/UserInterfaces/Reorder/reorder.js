// The page of the reorder task : window.fantoche is handed over by the application, see
// IUserInterfaceTask.
const list = document.getElementById('items');
const error = document.getElementById('error');
let dragged = null;

document.getElementById('title').textContent = fantoche.input.Title;
for (const item of fantoche.input.Items ?? []) {
    const row = document.createElement('li');
    row.textContent = item;
    row.draggable = true;
    list.appendChild(row);
}

list.addEventListener('dragstart', event => {
    dragged = event.target;
    dragged.classList.add('dragged');
});

list.addEventListener('dragend', () => {
    dragged.classList.remove('dragged');
    dragged = null;
});

// The dragged row moves before the row it is over, or after it past its middle.
list.addEventListener('dragover', event => {
    event.preventDefault();
    const over = event.target.closest('li');
    if (!dragged || !over || over === dragged)
        return;

    const box = over.getBoundingClientRect();
    const isPastMiddle = event.clientY > box.top + box.height / 2;
    list.insertBefore(dragged, isPastMiddle ? over.nextSibling : over);
});

document.getElementById('confirm').addEventListener('click', async () => {
    const items = [...list.children].map(row => row.textContent);
    try {
        await fantoche.answer({ Items: items });
    } catch (refused) {
        error.textContent = refused.message;
        error.hidden = false;
    }
});

document.getElementById('cancel').addEventListener('click', () => fantoche.close());
