using System;
using System.Collections.Generic;

namespace OperationGuidance_service.Models.DTOs {
    /// <summary>
    /// Represents a paginated result set containing data and pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of data in the result set.</typeparam>
    public class PagedResult<T> {
        /// <summary>
        /// The data items for the current page.
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The total number of pages.
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>
        /// Indicates whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Indicates whether there is a next page.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Parameters for pagination requests.
    /// </summary>
    public class PaginationParams {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        /// <summary>
        /// The page number to retrieve (1-based). Defaults to 1.
        /// </summary>
        public int PageNumber {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        /// <summary>
        /// The number of items per page. Defaults to 10.
        /// </summary>
        public int PageSize {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > 1000 ? 1000 : value);
        }

        /// <summary>
        /// The column name to order by. Defaults to "id" if not specified.
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort in descending order. Defaults to false (ascending).
        /// </summary>
        public bool Descending { get; set; } = false;

        /// <summary>
        /// Calculates the offset for SQL queries.
        /// </summary>
        public int Offset => (PageNumber - 1) * PageSize;
    }
}
