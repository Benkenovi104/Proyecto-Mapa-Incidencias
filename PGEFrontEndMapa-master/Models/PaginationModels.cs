namespace IntegrarMapa.Models
{
    public class PagedResponse<T>
    {
        public List<T> Incidencias { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public bool HasNextPage { get; set; } = false;
        public bool HasPreviousPage { get; set; } = false;

        // Propiedades computadas útiles
        public string DisplayText => $"Página {CurrentPage} de {TotalPages} - {TotalCount} incidencias totales";
        public bool HasPages => TotalPages > 1;
    }
}