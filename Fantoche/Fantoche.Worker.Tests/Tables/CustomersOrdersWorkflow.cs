namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The practical workflow of the table tasks, as the samples of the application hold it (see
/// DatabaseSeeder) less its pages : the columns are mapped and the document composed in the
/// mappings, so it runs unattended.
/// <code>
/// Start ─┬─> Import customers ─> Customers (map) ─┬─> Tables (join) ─> Merge ─> Update ─> Split ─> Export ─> End
///        └─> Import orders ────> Orders (map) ────┘
/// </code>
/// The files name their columns their own way ("client id", "client", "total") : the workflow reads
/// them by the names of the mapping.
/// </summary>
internal static class CustomersOrdersWorkflow
{
    public const string CustomersHeader = "client id;full name;town";
    public const string OrdersHeader = "order no,client,total,ordered on";

    public static TestWorkflow Build(int batchSize = 10000) => new TestWorkflow("Customers and orders", requireOutput: false)
        .Start()
        .Task("Import customers", PluginTasks.ImportCsv, new { Path = "$previous.Customers" })
        .Task("Customers", PluginTasks.MapColumns, new
        {
            Source = "$previous",
            Columns = new[] { Column("CustomerId", "client id"), Column("Name", "full name"), Column("City", "town") }
        })
        .Task("Import orders", PluginTasks.ImportCsv, new { Path = "$previous.Orders" })
        .Task("Orders", PluginTasks.MapColumns, new
        {
            Source = "$previous",
            Columns = new[] { Column("OrderId", "order no"), Column("CustomerId", "client"), Column("Amount", "total"), Column("Date", "ordered on") }
        })
        .Join("Tables", new { Customers = "$previous.Customers", Orders = "$previous.Orders" })
        .Task("Merge", PluginTasks.Merge, new
        {
            Left = "$previous.Orders",
            Right = "$previous.Customers",
            On = new[] { new { Left = "$previous.Orders.Mapping.CustomerId", Right = "$previous.Customers.Mapping.CustomerId" } },
        })
        .Task("Update", PluginTasks.Update, new
        {
            Source = "$previous",
            Conditions = new[] { new { Left = "$row.columns.Amount", Operator = "GreaterOrEqual", Right = 500 } },
            Set = new[] { new { Column = "Priority", Value = (object)"high" } },
        })
        .Task("Split", PluginTasks.Split, new { Source = "$previous", BatchSize = batchSize })
        .Task("Export", PluginTasks.ExportPdf, new
        {
            Sources = "$previous.Batches",
            FileName = "orders-{index}",
            Document = new
            {
                Landscape = true,
                Blocks = new object[]
                {
                    new { Kind = "Heading", Text = "Orders, part {index} of {count}" },
                    new { Kind = "Text", Text = "{rows} orders, the large ones flagged." },
                    new
                    {
                        Kind = "Table",
                        Columns = new[] { new { Column = "OrderId", Header = "Order" }, new { Column = "Date", Header = "" }, new { Column = "Name", Header = "Customer" }, new { Column = "City", Header = "" }, new { Column = "Amount", Header = "" }, new { Column = "Priority", Header = "" } }
                    },
                }
            }
        })
        .End(new { Files = "$previous.Files" })
        .Chain("Start", "Import customers", "Customers", "Tables")
        .Chain("Start", "Import orders", "Orders", "Tables")
        .Chain("Tables", "Merge", "Update", "Split", "Export", "End");

    private static object Column(string name, string column) => new { Name = name, Column = column };

    /// <summary>[count] customers, numbered from 1, in [cities] cities.</summary>
    public static void WriteCustomers(string path, int count)
    {
        using StreamWriter writer = new(path);
        writer.WriteLine(CustomersHeader);
        for (int i = 1; i <= count; i++)
            writer.WriteLine($"{i};Customer {i};City {i % 50}");
    }

    /// <summary>[count] orders spread over the [customers], their amounts going round from 0 to 999.</summary>
    public static void WriteOrders(string path, int count, int customers)
    {
        using StreamWriter writer = new(path);
        writer.WriteLine(OrdersHeader);
        DateOnly start = new(2026, 1, 1);
        for (int i = 0; i < count; i++)
            writer.WriteLine($"O{i},{i % customers + 1},{i % 1000},{start.AddDays(i % 365):yyyy-MM-dd}");
    }
}
