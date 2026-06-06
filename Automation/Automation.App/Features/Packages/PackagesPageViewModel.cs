using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Automation.Shared.Data.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Packages;

public partial class PackagesPageViewModel : ObservableObject, INavigable
{
    private readonly List<PackageInfos> _all = new();
    private List<PackageInfos> _filtered = new();

    public PackagesPageViewModel()
    {
        _all = Load().ToList();
        ApplyFilter();
    }

    public ObservableCollection<PackageInfos> Items { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 100;

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        ApplyFilter();
    }

    partial void OnCurrentPageChanged(int value) => UpdatePageItems();

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = _all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(p =>
                p.Identifier.Identifier.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        _filtered = query.ToList();
        TotalItems = _filtered.Count;
        UpdatePageItems();
    }

    private void UpdatePageItems()
    {
        var page = _filtered
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        Items.Clear();
        foreach (var p in page)
            Items.Add(p);
    }

    [RelayCommand]
    private void Add() { /* open add dialog, then ApplyFilter() */ }

    [RelayCommand]
    private void Edit(PackageInfos package) { /* edit selected */ }

    [RelayCommand]
    private void Remove(PackageInfos package)
    {
        _all.Remove(package);
        ApplyFilter();
    }

    private static IEnumerable<PackageInfos> Load() => Enumerable.Empty<PackageInfos>();
}