// The page of the table review task : window.fantoche is handed over by the application, see
// IUserInterfaceTask.
const PageSize = 100;
const table = fantoche.input.Table;
const error = document.getElementById('error');
const previous = document.getElementById('previous');
const next = document.getElementById('next');
let offset = 0;

document.getElementById('title').textContent = fantoche.input.Title;

function showError(message) {
    error.textContent = message;
    error.hidden = false;
}

function render(page) {
    const columns = document.getElementById('columns');
    columns.replaceChildren(...page.Columns.map(column => {
        const header = document.createElement('th');
        header.textContent = column.Name;
        header.title = column.Type;
        return header;
    }));

    const rows = document.getElementById('rows');
    rows.replaceChildren(...page.Rows.map(row => {
        const line = document.createElement('tr');
        for (const column of page.Columns) {
            const cell = document.createElement('td');
            const value = row[column.Name];
            cell.textContent = value === null || value === undefined ? '' : String(value);
            line.appendChild(cell);
        }
        return line;
    }));

    const last = offset + page.Rows.length;
    document.getElementById('range').textContent = page.Total === 0
        ? 'No rows'
        : `Rows ${offset + 1}–${last} of ${page.Total}`;
    previous.disabled = offset === 0;
    next.disabled = last >= page.Total;
}

async function load(from) {
    try {
        const page = await fantoche.readTable(table, { offset: from, limit: PageSize });
        offset = from;
        render(page);
    } catch (refused) {
        showError(refused.message);
    }
}

previous.addEventListener('click', () => load(Math.max(0, offset - PageSize)));
next.addEventListener('click', () => load(offset + PageSize));

document.getElementById('confirm').addEventListener('click', async () => {
    try {
        await fantoche.answer({ Table: table });
    } catch (refused) {
        showError(refused.message);
    }
});

document.getElementById('cancel').addEventListener('click', () => fantoche.close());

load(0);
