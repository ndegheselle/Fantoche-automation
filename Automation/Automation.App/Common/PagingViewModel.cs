using Automation.Shared.Base;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Common
{
    /// <summary>
    /// The page of a list being displayed : moving to another one or resizing it reloads what is
    /// displayed.
    /// <para>
    /// Held by the view model rather than inherited from : an overlay already has a base of its
    /// own, and paging is something a view model has rather than something it is.
    /// </para>
    /// </summary>
    public partial class PagingViewModel : ObservableObject
    {
        [ObservableProperty] private int _pageNumber = 1;
        [ObservableProperty] private int _capacity = 50;

        /// <summary>The page currently asked for, as the services take it.</summary>
        public PaginationOptions Options => new() { Page = PageNumber, PageSize = Capacity };

        /// <summary>Read the page again and display what comes back.</summary>
        protected readonly Func<Task> Refresh;

        public PagingViewModel(Func<Task> refresh)
        {
            Refresh = refresh;
        }

        partial void OnPageNumberChanged(int value) => _ = Refresh();

        partial void OnCapacityChanged(int value) => _ = Refresh();
    }

    /// <summary>
    /// A <see cref="PagingViewModel"/> whose list is searched, a new search going back to the first
    /// page : the results about to be read are not the ones that page was counted in.
    /// </summary>
    public partial class SearchedPagingViewModel : PagingViewModel
    {
        [ObservableProperty] private string _search = "";

        public SearchedPagingViewModel(Func<Task> refresh) : base(refresh)
        { }

        partial void OnSearchChanged(string value)
        {
            // Going back to the first page reloads on its own, so only a search already on it has
            // anything left to do.
            if (PageNumber != 1)
                PageNumber = 1;
            else
                _ = Refresh();
        }
    }
}
