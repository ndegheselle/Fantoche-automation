// The page of the pick columns task : window.fantoche is handed over by the application, see
// IUserInterfaceTask. The logic works on a plain state, the DOM only shows it and edits it.
const PreviewSize = 20;
const source = fantoche.input.Source;
const mapping = source.Mapping ?? {};

const error = document.getElementById('error');
let state = null;

document.getElementById('title').textContent = fantoche.input.Title;

// ---- Logic

/** [text] compared loosely : "Client ID", "client_id" and "clientid" read the same. */
function normalized(text) {
    return (text ?? '')
        .normalize('NFD')
        .replace(/[̀-ͯ]/g, '')
        .toLowerCase()
        .replace(/[^a-z0-9]/g, '');
}

/** The stored column [column] stands for : a name of the mapping of the table, or the column itself. */
function storedColumnOf(column, columns) {
    const stored = mapping[column] ?? column;
    return columns.includes(stored) ? stored : '';
}

/** The column [name] most likely stands for : the one asked for, then the one named alike, then none. */
function suggestedColumn(expected, columns, taken) {
    const asked = storedColumnOf(expected.Column, columns);
    if (asked)
        return asked;

    const name = normalized(expected.Name);
    const free = columns.filter(column => !taken.includes(column));
    return free.find(column => normalized(column) === name)
        ?? free.find(column => name.length > 2 && normalized(column).includes(name))
        ?? '';
}

/** The name a stored column already has in the mapping of the table, empty when it has none. */
function mappedNameOf(column) {
    return Object.keys(mapping).find(name => mapping[name] === column) ?? '';
}

function initialState(columns) {
    const expected = [];
    for (const asked of fantoche.input.Columns ?? []) {
        const taken = expected.map(x => x.Column);
        expected.push({ Name: asked.Name, Column: suggestedColumn(asked, columns, taken) });
    }

    const named = expected.map(x => x.Name);
    const others = {};
    for (const column of columns) {
        const name = mappedNameOf(column);
        others[column] = named.includes(name) ? '' : name;
    }
    return { columns, expected, others };
}

/** The columns no name of the workflow picked, offered to be named. */
function otherColumns(current) {
    const picked = current.expected.map(x => x.Column);
    return current.columns.filter(column => !picked.includes(column));
}

/** What the task is answered with : every name of the workflow, then the other columns named. */
function answerOf(current) {
    const columns = current.expected.map(x => ({ Name: x.Name, Column: x.Column }));
    for (const column of otherColumns(current)) {
        const name = current.others[column].trim();
        if (name)
            columns.push({ Name: name, Column: column });
    }
    return { Source: source, Columns: columns };
}

/** Why the answer can't be confirmed : names left without a column, a name or a column given twice. */
function errorsOf(current) {
    const errors = [];
    for (const expected of current.expected) {
        if (!expected.Column)
            errors.push(`Pick the column of [${expected.Name}].`);
    }

    const answer = answerOf(current).Columns;
    const seenNames = new Set();
    const seenColumns = new Set();
    for (const column of answer) {
        const name = column.Name.toLowerCase();
        if (seenNames.has(name))
            errors.push(`The name [${column.Name}] is given twice.`);
        if (column.Column && seenColumns.has(column.Column))
            errors.push(`The column [${column.Column}] is picked twice.`);
        seenNames.add(name);
        seenColumns.add(column.Column);
    }
    return errors;
}

// ---- Display

function cell(content, className) {
    const td = document.createElement('td');
    if (className)
        td.className = className;
    if (content instanceof Node)
        td.appendChild(content);
    else
        td.textContent = content ?? '';
    return td;
}

function textOf(value) {
    return value === null || value === undefined ? '' : String(value);
}

function renderExpected(page) {
    const body = document.getElementById('expected');
    body.replaceChildren(...state.expected.map(expected => {
        const select = document.createElement('select');
        select.append(new Option('(pick a column)', ''), ...state.columns.map(column => new Option(column, column)));
        select.value = expected.Column;
        select.addEventListener('change', () => {
            expected.Column = select.value;
            render(page);
        });

        const row = document.createElement('tr');
        row.append(
            cell(expected.Name),
            cell(select, expected.Column ? '' : 'missing'),
            cell(textOf(page.Rows[0]?.[expected.Column]), 'sample'));
        return row;
    }));
}

function renderOthers(page) {
    const body = document.getElementById('others');
    body.replaceChildren(...otherColumns(state).map(column => {
        const input = document.createElement('input');
        input.value = state.others[column];
        input.placeholder = 'read under its own name';
        input.addEventListener('input', () => {
            state.others[column] = input.value;
            renderPreview(page);
        });

        const row = document.createElement('tr');
        row.append(cell(column), cell(input), cell(textOf(page.Rows[0]?.[column]), 'sample'));
        return row;
    }));
}

function renderPreview(page) {
    const names = {};
    for (const column of answerOf(state).Columns)
        names[column.Column] = column.Name;

    document.getElementById('columns').replaceChildren(...state.columns.map(column => {
        const header = document.createElement('th');
        header.textContent = names[column] ?? column;
        if (names[column] && names[column] !== column) {
            const stored = document.createElement('small');
            stored.textContent = column;
            header.appendChild(stored);
        }
        return header;
    }));

    document.getElementById('rows').replaceChildren(...page.Rows.map(row => {
        const line = document.createElement('tr');
        line.append(...state.columns.map(column => cell(textOf(row[column]))));
        return line;
    }));

    const mapped = answerOf(state).Columns.length;
    document.getElementById('summary').textContent = `${mapped} of ${state.columns.length} columns named, ${page.Total} rows`;
}

function render(page) {
    renderExpected(page);
    renderOthers(page);
    renderPreview(page);
}

function showError(message) {
    error.textContent = message;
    error.hidden = false;
}

async function load() {
    try {
        const page = await fantoche.readTable(source, { offset: 0, limit: PreviewSize });
        state = initialState(page.Columns.map(column => column.Name));
        render(page);
    } catch (refused) {
        showError(refused.message);
    }
}

document.getElementById('confirm').addEventListener('click', async () => {
    if (!state)
        return;

    error.hidden = true;
    const errors = errorsOf(state);
    if (errors.length > 0) {
        showError(errors.join('\n'));
        return;
    }

    try {
        await fantoche.answer(answerOf(state));
    } catch (refused) {
        showError(refused.message);
    }
});

document.getElementById('cancel').addEventListener('click', () => fantoche.close());

load();
