// The page of the compose PDF task : window.fantoche is handed over by the application, see
// IUserInterfaceTask. The logic works on the document as it is answered, the DOM only shows it and
// edits it. What the preview shows follows what PdfRenderer draws.
const PreviewSize = 20;
const tables = fantoche.input.Tables ?? [];
const sample = tables[0];

const error = document.getElementById('error');
let columns = [];
let page = { Columns: [], Total: 0, Rows: [] };
let composed = null;

document.getElementById('title').textContent = fantoche.input.Title;

// ---- Logic

/** The name the workflow reads a stored column by : the one its table maps to it, or its own. */
function nameOf(table, column) {
    const mapping = table?.Mapping ?? {};
    return Object.keys(mapping).find(name => mapping[name] === column) ?? column;
}

function storedColumnOf(table, name) {
    return table?.Mapping?.[name] ?? name;
}

function allColumns() {
    return columns.map(column => ({ Column: column, Header: '' }));
}

/** What the editor starts from : the document given, or a heading named after the title over the whole table. */
function initialDocument(given) {
    if (given?.Blocks?.length > 0)
        return structuredClone(given);

    return {
        Landscape: given?.Landscape ?? false,
        Blocks: [
            { Kind: 'Heading', Text: `${fantoche.input.Title || 'Document'}, part {index} of {count}`, Columns: [] },
            { Kind: 'Text', Text: '{rows} rows', Columns: [] },
            { Kind: 'Table', Text: '', Columns: allColumns() },
        ],
    };
}

function newBlock(kind) {
    return { Kind: kind, Text: kind === 'Table' ? '' : kind, Columns: kind === 'Table' ? allColumns() : [] };
}

function move(list, index, offset) {
    const target = index + offset;
    if (target < 0 || target >= list.length)
        return;
    [list[index], list[target]] = [list[target], list[index]];
}

/** The placeholders of a text as they read in the first document. */
function previewText(text) {
    return (text ?? '')
        .replaceAll('{index}', '1')
        .replaceAll('{count}', String(tables.length))
        .replaceAll('{rows}', String(page.Total));
}

/** The columns a table block shows : the ones it names, every one when it names none. */
function shownColumns(block) {
    return block.Columns.length > 0 ? block.Columns : allColumns();
}

function errorsOf(current) {
    const errors = [];
    if (current.Blocks.length === 0)
        errors.push('Add at least a block to the document.');
    return errors;
}

// ---- Display

function element(tag, properties = {}, ...children) {
    const node = Object.assign(document.createElement(tag), properties);
    node.append(...children);
    return node;
}

function textOf(value) {
    return value === null || value === undefined ? '' : String(value);
}

function blockHeader(block, index) {
    const up = element('button', { type: 'button', textContent: '↑', title: 'Move up' });
    up.addEventListener('click', () => { move(composed.Blocks, index, -1); render(); });
    const down = element('button', { type: 'button', textContent: '↓', title: 'Move down' });
    down.addEventListener('click', () => { move(composed.Blocks, index, 1); render(); });
    const remove = element('button', { type: 'button', textContent: '✕', title: 'Remove' });
    remove.addEventListener('click', () => { composed.Blocks.splice(index, 1); render(); });
    return element('header', {}, element('strong', { textContent: block.Kind }), up, down, remove);
}

function columnsEditor(block) {
    const editor = element('div', { className: 'columns' });
    for (const column of columns) {
        const shown = block.Columns.find(x => x.Column === column);
        const box = element('input', { type: 'checkbox', checked: Boolean(shown) });
        const header = element('input', { type: 'text', value: shown?.Header ?? '', placeholder: column, disabled: !shown });

        box.addEventListener('change', () => {
            if (box.checked) {
                // Kept in the order of the table, whatever order they are ticked in.
                block.Columns.push({ Column: column, Header: header.value });
                block.Columns.sort((a, b) => columns.indexOf(a.Column) - columns.indexOf(b.Column));
            } else {
                block.Columns = block.Columns.filter(x => x.Column !== column);
            }
            render();
        });
        header.addEventListener('input', () => {
            const entry = block.Columns.find(x => x.Column === column);
            if (entry)
                entry.Header = header.value;
            renderPreview();
        });
        editor.append(element('label', {}, box, ` ${column}`), header);
    }
    return editor;
}

function blockEditor(block, index) {
    const item = element('li', { className: 'block' }, blockHeader(block, index));
    if (block.Kind === 'Table') {
        item.append(columnsEditor(block));
        return item;
    }

    const input = block.Kind === 'Heading'
        ? element('input', { type: 'text', value: block.Text })
        : element('textarea', { value: block.Text });
    input.addEventListener('input', () => { block.Text = input.value; renderPreview(); });
    item.append(input);
    return item;
}

function previewTable(block) {
    const shown = shownColumns(block);
    const headers = shown.map(x => element('th', { textContent: x.Header || x.Column }));
    const rows = page.Rows.map(row => element('tr', {},
        ...shown.map(x => element('td', { textContent: textOf(row[storedColumnOf(sample, x.Column)]) }))));

    const nodes = [element('table', {}, element('thead', {}, element('tr', {}, ...headers)), element('tbody', {}, ...rows))];
    const more = page.Total - page.Rows.length;
    if (more > 0)
        nodes.push(element('div', { className: 'more', textContent: `… and ${more} more rows` }));
    return nodes;
}

function renderPreview() {
    const sheet = document.getElementById('page');
    sheet.classList.toggle('landscape', composed.Landscape);
    sheet.replaceChildren(...composed.Blocks.flatMap(block => {
        if (block.Kind === 'Heading')
            return [element('h2', { textContent: previewText(block.Text) })];
        if (block.Kind === 'Text')
            return [element('p', { textContent: previewText(block.Text) })];
        return previewTable(block);
    }));
    document.getElementById('summary').textContent = `${tables.length} documents, the first one of ${page.Total} rows`;
}

function render() {
    document.getElementById('landscape').checked = composed.Landscape;
    document.getElementById('blocks').replaceChildren(...composed.Blocks.map(blockEditor));
    renderPreview();
}

function showError(message) {
    error.textContent = message;
    error.hidden = false;
}

async function load() {
    try {
        if (sample)
            page = await fantoche.readTable(sample, { offset: 0, limit: PreviewSize });
        columns = page.Columns.map(column => nameOf(sample, column.Name));
        page.Rows = page.Rows ?? [];
    } catch (refused) {
        showError(refused.message);
    }
    composed = initialDocument(fantoche.input.Document);
    render();
}

document.getElementById('landscape').addEventListener('change', event => {
    composed.Landscape = event.target.checked;
    renderPreview();
});

for (const button of document.querySelectorAll('.add button')) {
    button.addEventListener('click', () => {
        composed.Blocks.push(newBlock(button.dataset.kind));
        render();
    });
}

document.getElementById('confirm').addEventListener('click', async () => {
    error.hidden = true;
    const errors = errorsOf(composed);
    if (errors.length > 0) {
        showError(errors.join(' '));
        return;
    }

    try {
        await fantoche.answer({ Tables: tables, Document: composed });
    } catch (refused) {
        showError(refused.message);
    }
});

document.getElementById('cancel').addEventListener('click', () => fantoche.close());

load();
