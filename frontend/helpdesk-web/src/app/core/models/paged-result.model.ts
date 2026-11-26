/** Página de resultados (PagedResult&lt;T&gt; de la API). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
