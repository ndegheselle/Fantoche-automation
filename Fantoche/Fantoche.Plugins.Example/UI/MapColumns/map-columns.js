// The page of the map columns task : window.fantoche is handed over by the application, see
// IUserInterfaceTask. The page edits the parameters of the task, the task suggesting the columns
// and checking the answer (its page methods Suggest and Check). The logic works on a plain state,
// the DOM only shows it and edits it.
const PreviewSize = 20;
const source = fantoche.input.Source;
const mapping = source.Mapping ?? {};

const error = document.getElementById('error');
let state = null;

document.getElementById('title').textContent = `Columns of ${source.Name || 'the table'}`;

// ---- Logic

/** The name a stored column already has in the mapping of the table, empty when it has none. */
function mappedNameOf(column) {
    return Object.keys(mapping).find(name => mapping[name] === column) ?? '';
}

/** [suggested] being the column the task suggests for each name of the workflow. */
function initialState(columns, suggested) {
    const expected = suggested.map(x => ({ Name: x.Name, Column: x.Column }));

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

/**
 * What the task is answered with : its parameters, the columns being every name of the workflow,
 * then the other columns named.
 */
function answerOf(current) {
    const columns = current.expected.map(x => ({ Name: x.Name, Column: x.Column }));
    for (const column of otherColumns(current)) {
        const name = current.others[column].trim();
        if (name)
            columns.push({ Name: name, Column: column });
    }
    return { ...fantoche.input, Columns: columns };
}

/** The names left without a column : the rest is checked by the task (see Check). */
function missingColumnsOf(current) {
    return current.expected.filter(x => !x.Column).map(x => `Pick the column of [${x.Name}].`);
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
        // Pico outlines a field marked invalid.
        if (!expected.Column)
            select.setAttribute('aria-invalid', 'true');
        select.addEventListener('change', () => {
            expected.Column = select.value;
            render(page);
        });

        const row = document.createElement('tr');
        row.append(
            cell(expected.Name),
            cell(select),
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
        const suggested = await fantoche.call('Suggest', fantoche.input);
        state = initialState(page.Columns.map(column => column.Name), suggested);
        render(page);
    } catch (refused) {
        showError(refused.message);
    }
}

document.getElementById('confirm').addEventListener('click', async () => {
    if (!state)
        return;

    error.hidden = true;
    const missing = missingColumnsOf(state);
    if (missing.length > 0) {
        showError(missing.join('\n'));
        return;
    }

    try {
        const answer = answerOf(state);
        const refusal = await fantoche.call('Check', answer);
        if (refusal) {
            showError(refusal);
            return;
        }
        await fantoche.answer(answer);
    } catch (refused) {
        showError(refused.message);
    }
});

document.getElementById('cancel').addEventListener('click', () => fantoche.close());

load();
