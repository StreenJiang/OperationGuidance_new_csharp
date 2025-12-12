using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.AbstractClasses {
    /// <summary>
    /// Base abstract class for paginated requests, providing common pagination parameters with validation.
    /// </summary>
    public abstract class PaginationRequestBase: HttpRequest {
        private int _pageNumber = 1;
        private int _pageSize = 50;

        /// <summary>
        /// The page number to retrieve (1-based). Defaults to 1.
        /// Range: 1 to int.MaxValue
        /// </summary>
        public int PageNumber {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        /// <summary>
        /// The number of items per page. Defaults to 50.
        /// Range: 1 to 1000
        /// </summary>
        public int PageSize {
            get => _pageSize;
            set => _pageSize = value < 1 ? 50 : (value > 1000 ? 1000 : value);
        }

        /// <summary>
        /// Converts pagination parameters to PaginationParams for database queries.
        /// </summary>
        /// <param name="defaultOrderBy">Default column name to order by if not specified</param>
        /// <param name="defaultDescending">Default sort order (true for descending)</param>
        /// <returns>PaginationParams instance configured with current values</returns>
        public PaginationParams ToPaginationParams(
            string defaultOrderBy = "id",
            bool defaultDescending = false) {

            return new PaginationParams {
                PageNumber = PageNumber,
                PageSize = PageSize,
                OrderBy = defaultOrderBy,
                Descending = defaultDescending
            };
        }

        /// <summary>
        /// Calculates the offset for SQL queries.
        /// </summary>
        public int Offset => (PageNumber - 1) * PageSize;
    }
}
