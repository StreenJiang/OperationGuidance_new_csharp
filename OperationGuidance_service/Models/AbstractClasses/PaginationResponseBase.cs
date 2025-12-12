using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.AbstractClasses {
    /// <summary>
    /// Base abstract class for paginated responses, providing PagedResult<T> and backward compatibility.
    /// </summary>
    /// <typeparam name="T">The type of data in the result set.</typeparam>
    public abstract class PaginationResponseBase<T>: HttpResponse {
        private readonly PagedResult<T> _pagedResult = new();

        /// <summary>
        /// The paginated result containing data and pagination metadata.
        /// </summary>
        public PagedResult<T> PagedResult {
            get => _pagedResult;
            init => _pagedResult = value ?? new PagedResult<T>();
        }

        /// <summary>
        /// The data items for the current page.
        /// Provides backward compatibility for existing code expecting List<T>.
        /// </summary>
        public List<T> Items => PagedResult?.Data ?? new List<T>();

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public int TotalCount => PagedResult?.TotalCount ?? 0;

        /// <summary>
        /// The current page number (1-based).
        /// </summary>
        public int PageNumber => PagedResult?.PageNumber ?? 1;

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize => PagedResult?.PageSize ?? 0;

        /// <summary>
        /// The total number of pages.
        /// </summary>
        public int TotalPages => PagedResult?.TotalPages ?? 0;

        /// <summary>
        /// Indicates whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => PagedResult?.HasPreviousPage ?? false;

        /// <summary>
        /// Indicates whether there is a next page.
        /// </summary>
        public bool HasNextPage => PagedResult?.HasNextPage ?? false;

        /// <summary>
        /// Default constructor that initializes PagedResult.
        /// </summary>
        protected PaginationResponseBase() {
            PagedResult = new PagedResult<T>();
        }

        /// <summary>
        /// Initializes the response with a PagedResult.
        /// </summary>
        /// <param name="pagedResult">The paginated result to use</param>
        protected PaginationResponseBase(PagedResult<T> pagedResult) {
            PagedResult = pagedResult ?? new PagedResult<T>();
        }

        /// <summary>
        /// Convenience method to set the paginated result data.
        /// </summary>
        /// <param name="data">The data items</param>
        /// <param name="totalCount">The total count</param>
        /// <param name="pageNumber">The current page number</param>
        /// <param name="pageSize">The page size</param>
        public void SetPagedData(List<T> data, int totalCount, int pageNumber, int pageSize) {
            PagedResult.Data = data ?? new List<T>();
            PagedResult.TotalCount = totalCount;
            PagedResult.PageNumber = pageNumber;
            PagedResult.PageSize = pageSize;
        }
    }
}
