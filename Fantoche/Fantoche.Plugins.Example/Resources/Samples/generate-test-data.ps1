<#
.SYNOPSIS
    Generate larger customers and orders files for the "Customers and orders" sample workflow.

.DESCRIPTION
    The files have the headers and delimiters of customers.csv and orders.csv, so the column
    picks of the workflow work the same on them. A few orders reference customers that don't
    exist, which the inner merge of the workflow drops. About half the orders are at or above
    500, the threshold the workflow flags as high priority.

    The output depends only on -Seed, so a run can be reproduced.

.EXAMPLE
    ./generate-test-data.ps1 -Customers 5000 -Orders 250000
    Then start the workflow with the paths of generated/customers.csv and generated/orders.csv.
#>
param(
    [int] $Customers = 1000,
    [int] $Orders = 100000,
    [string] $OutputFolder = (Join-Path $PSScriptRoot "generated"),
    [int] $Seed = 42,
    # Share of the orders pointing to no customer.
    [double] $OrphanRate = 0.02
)

$ErrorActionPreference = "Stop"

$firstNames = @("Alice", "Bruno", "Chloé", "David", "Emma", "Félix", "Gabrielle", "Hugo", "Inès", "Jules",
    "Léa", "Louis", "Manon", "Noé", "Océane", "Paul", "Rose", "Sacha", "Théo", "Zoé")
$lastNames = @("Martin", "Petit", "Bernard", "Dubois", "Laurent", "Moreau", "Simon", "Michel", "Lefèvre", "Garcia",
    "Roux", "Fournier", "Girard", "Bonnet", "Dupont", "Lambert", "Fontaine", "Rousseau", "Vincent", "Mercier")
$towns = @("Paris", "Lyon", "Lille", "Nantes", "Bordeaux", "Toulouse", "Nice", "Strasbourg", "Rennes", "Montpellier")

$random = New-Object System.Random $Seed
New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
# Without a BOM, the first column name is read as is.
$encoding = New-Object System.Text.UTF8Encoding $false

function Pick($items) { $items[$random.Next($items.Count)] }

$customersPath = Join-Path $OutputFolder "customers.csv"
$writer = New-Object System.IO.StreamWriter $customersPath, $false, $encoding
try {
    $writer.WriteLine("client id;full name;town")
    for ($id = 1; $id -le $Customers; $id++) {
        $writer.WriteLine("$id;$(Pick $firstNames) $(Pick $lastNames);$(Pick $towns)")
    }
}
finally {
    $writer.Dispose()
}

$ordersPath = Join-Path $OutputFolder "orders.csv"
$firstDay = [datetime]"2026-01-01"
$writer = New-Object System.IO.StreamWriter $ordersPath, $false, $encoding
try {
    $writer.WriteLine("order no,client,total,ordered on")
    for ($index = 1; $index -le $Orders; $index++) {
        $client = if ($random.NextDouble() -lt $OrphanRate) { $Customers + 1 + $random.Next(1000) } else { 1 + $random.Next($Customers) }
        $total = 10 + $random.Next(990)
        $orderedOn = $firstDay.AddDays($random.Next(365)).ToString("yyyy-MM-dd")
        $writer.WriteLine("A$($index + 1000),$client,$total,$orderedOn")
    }
}
finally {
    $writer.Dispose()
}

Write-Host "Customers : $customersPath ($Customers rows)"
Write-Host "Orders    : $ordersPath ($Orders rows)"
