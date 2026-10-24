// The page of the files review task : window.fantoche is handed over by the application, see
// IUserInterfaceTask.
const files = fantoche.input.Files ?? [];
const list = document.getElementById('files');
const preview = document.getElementById('preview');
const error = document.getElementById('error');
const count = document.getElementById('count');

document.getElementById('title').textContent = fantoche.input.Title;

function keptFiles() {
    const boxes = [...list.querySelectorAll('input[type=checkbox]')];
    return files.filter((_, index) => boxes[index].checked);
}

function showCount() {
    count.textContent = `${keptFiles().length} of ${files.length} kept`;
}

function select(row, file) {
    list.querySelector('.selected')?.classList.remove('selected');
    row.classList.add('selected');
    preview.src = fantoche.fileUrl(file);
}

for (const file of files) {
    const row = document.createElement('li');
    const box = document.createElement('input');
    box.type = 'checkbox';
    box.checked = true;
    box.addEventListener('change', showCount);
    const name = document.createElement('button');
    name.type = 'button';
    name.className = 'name';
    name.textContent = file.Name;
    name.addEventListener('click', () => select(row, file));
    row.append(box, name);
    list.appendChild(row);
}

if (files.length > 0)
    select(list.firstElementChild, files[0]);
showCount();

document.getElementById('confirm').addEventListener('click', async () => {
    try {
        await fantoche.answer({ Files: keptFiles() });
    } catch (refused) {
        error.textContent = refused.message;
        error.hidden = false;
    }
});

document.getElementById('cancel').addEventListener('click', () => fantoche.close());
