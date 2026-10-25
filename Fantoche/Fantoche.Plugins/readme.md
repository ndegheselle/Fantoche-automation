# Table tasks

`Fantoche.Plugins` holds the tasks of a practical workflow : two CSV files imported, given clear column names, merged, updated, split in batches and exported to PDF. The samples of a fresh install hold it as **Customers and orders**.

```text
Start ─┬─> Import CSV ─> Pick columns (user) ─> Map columns ─┬─> Join ─> Merge ─> Update ─> Split ─> Compose PDF (user) ─> Export PDF ─> End
       └─> Import CSV ─> Pick columns (user) ─> Map columns ─┘
```

To run it : add the package (`dotnet pack Fantoche.Plugins`, version 2.0.0) from the Packages page, open the workflow and start it with the paths of `Samples/customers.csv` and `Samples/orders.csv`. The run waits for you at both picks and at the composition.

## The tasks

| Task | Does |
| --- | --- |
| Import CSV | A file into a table, the delimiter, columns and types detected from the content. The table is named after the file unless given a name. |
| Pick columns *(page)* | You pick the column of the file each name of the workflow stands for, and name the other ones if needed. |
| Map columns | Adds the names to the table (`TableRef.Mapping`), without copying it. |
| Merge | Joins two tables on pairs of columns (inner or left), in the order of the left table. A right column clashing with a left one is prefixed with the name of its table (`customers_Name`). |
| Update | Gives a value to a column of the rows matching conditions, or adds a column. |
| Split | Cuts a table into batches of `BatchSize` rows (10 000 by default). |
| Compose PDF *(page)* | You compose the document (headings, texts, tables), previewed on the first batch. |
| Export PDF | Saves a PDF per table, one per processor at once. |

A task done by the user comes before the task applying its answer, which takes the same parameters : a run without anyone to answer (a schedule, a test on large files) skips the page and writes them in the mapping instead. `CustomersOrdersWorkflow` in the tests is the sample written that way.

## Clear names for the columns

The names are the workflow's, written in the editor (`Pick columns` asks for them) : the nodes after are written against them, whatever the file calls its columns.

- `$previous.Mapping.CustomerId` is the stored column `CustomerId` stands for, offered by the editor wherever a column is expected (the keys of `Merge`, for instance).
- `$row.columns.CustomerId` reads it in the fields evaluated per row (the conditions and values of `Update`).
- A task creating a table from a mapped one stores the columns under their names and hands the mapping over, each name standing for itself : the references keep reading the same columns down the workflow.

The editor knows the names before anything runs because the tasks declare what they hand over of their input (see [What the editor previews of a task](../../architecture.md#what-the-editor-previews-of-a-task)).

## Performance

Each table operation is a single DuckDB query on the whole table, and the PDF are rendered side by side. Run the workflow on generated files with :

```shell
FANTOCHE_PERF_ORDERS=1000000 dotnet test Fantoche.Worker.Tests --filter "FullyQualifiedName~PerformanceTests"
```

100 000 customers merged with 1 000 000 orders, on 4 cores : about 15 s, 10 of them for the 100 PDF of 10 000 rows. With 5 000 000 orders : about 64 s, the time growing with the rows.
