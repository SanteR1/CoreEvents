import { Link } from 'react-router';

interface EventPaginationProps {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  filters: {
    title?: string;
    from?: string;
    to?: string;
  };
}

export const EventPagination = ({
  currentPage,
  totalPages,
  totalCount,
  pageSize,
  filters,
}: EventPaginationProps) => {
  const buildUrl = (page: number, newPageSize?: number) => {
    const params = new URLSearchParams();
    if (page > 1) {
      params.set('page', page.toString());
    }
    const size = newPageSize ?? pageSize;
    if (size !== 6) {
      params.set('pageSize', size.toString());
    }
    if (filters.title) {
      params.set('title', filters.title);
    }
    if (filters.from) {
      params.set('from', filters.from);
    }
    if (filters.to) {
      params.set('to', filters.to);
    }

    const query = params.toString();
    return query ? `/?${query}` : '/';
  };

  // Вычисляем диапазон номеров страниц
  const getPageNumbers = () => {
    const pages: (number | '...')[] = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible + 2) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      pages.push(1);
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);

      if (start > 2) {
        pages.push('...');
      }

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }

      if (end < totalPages - 1) {
        pages.push('...');
      }
      pages.push(totalPages);
    }

    return pages;
  };

  const startItem = Math.min((currentPage - 1) * pageSize + 1, totalCount);
  const endItem = Math.min(currentPage * pageSize, totalCount);

  return (
    <div className="flex flex-col items-center justify-between gap-4 py-4 text-left sm:flex-row">
      {/* Информация о количестве */}
      <div className="text-xs text-[var(--text)]">
        {totalCount > 0 ? (
          <span>
            Показано{' '}
            <strong className="font-semibold text-[var(--text-h)]">
              {startItem}–{endItem}
            </strong>{' '}
            из <strong className="font-semibold text-[var(--text-h)]">{totalCount}</strong> событий
          </span>
        ) : (
          <span>
            Страница {currentPage} из {totalPages || 1}
          </span>
        )}
      </div>

      {/* Кнопки переключения страниц */}
      {totalPages > 1 && (
        <div className="flex items-center gap-1">
          {/* Кнопка Назад */}
          {currentPage > 1 ? (
            <Link
              to={buildUrl(currentPage - 1)}
              className="flex h-9 items-center justify-center rounded-lg border border-[var(--border)] px-3 text-xs font-medium text-[var(--text-h)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              ← Назад
            </Link>
          ) : (
            <span className="flex h-9 cursor-not-allowed items-center justify-center rounded-lg border border-[var(--border)] px-3 text-xs font-medium text-[var(--text)] opacity-40">
              ← Назад
            </span>
          )}

          {/* Номера страниц */}
          <div className="flex items-center gap-1">
            {getPageNumbers().map((item, idx) => {
              if (item === '...') {
                return (
                  <span
                    key={idx < 3 ? 'dots-start' : 'dots-end'}
                    className="px-2 text-xs text-[var(--text)]"
                  >
                    ...
                  </span>
                );
              }

              const isCurrent = item === currentPage;
              return isCurrent ? (
                <span
                  key={item}
                  className="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-600 text-xs font-bold text-white shadow-sm"
                >
                  {item}
                </span>
              ) : (
                <Link
                  key={item}
                  to={buildUrl(item)}
                  className="flex h-9 w-9 items-center justify-center rounded-lg border border-[var(--border)] text-xs font-medium text-[var(--text-h)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  {item}
                </Link>
              );
            })}
          </div>

          {/* Кнопка Вперед */}
          {currentPage < totalPages ? (
            <Link
              to={buildUrl(currentPage + 1)}
              className="flex h-9 items-center justify-center rounded-lg border border-[var(--border)] px-3 text-xs font-medium text-[var(--text-h)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              Вперед →
            </Link>
          ) : (
            <span className="flex h-9 cursor-not-allowed items-center justify-center rounded-lg border border-[var(--border)] px-3 text-xs font-medium text-[var(--text)] opacity-40">
              Вперед →
            </span>
          )}
        </div>
      )}

      {/* Селектор размера страницы */}
      <div className="flex items-center gap-2 text-xs text-[var(--text)]">
        <span>Показывать по:</span>
        {[6, 12, 24].map((size) => (
          <Link
            key={size}
            to={buildUrl(1, size)}
            className={`rounded px-2 py-1 transition-colors ${
              pageSize === size
                ? 'bg-indigo-100 font-bold text-indigo-700 dark:bg-indigo-950/60 dark:text-indigo-300'
                : 'text-[var(--text)] hover:bg-gray-100 dark:hover:bg-gray-800'
            }`}
          >
            {size}
          </Link>
        ))}
      </div>
    </div>
  );
};
