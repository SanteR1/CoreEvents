import { Form, Link, useNavigation } from 'react-router';

interface EventFiltersProps {
  initialTitle?: string;
  initialFrom?: string;
  initialTo?: string;
  pageSize?: number;
}

export const EventFilters = ({
  initialTitle = '',
  initialFrom = '',
  initialTo = '',
  pageSize = 6,
}: EventFiltersProps) => {
  const navigation = useNavigation();
  const isSearching = navigation.state === 'loading' && Boolean(navigation.location.search);

  const hasActiveFilters = Boolean(initialTitle || initialFrom || initialTo);

  return (
    <div className="rounded-xl border border-[var(--border)] bg-[var(--bg)] p-5 text-left shadow-sm">
      <Form method="get" className="space-y-4">
        {/* Сохраняем текущий pageSize при фильтрации */}
        <input type="hidden" name="pageSize" value={pageSize} />
        {/* При смене фильтров сбрасываем на 1-ю страницу */}
        <input type="hidden" name="page" value={1} />

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {/* Поиск по названию */}
          <div className="space-y-1 sm:col-span-2 lg:col-span-2">
            <label
              htmlFor="title"
              className="block text-xs font-semibold tracking-wider text-[var(--text)] uppercase"
            >
              Название события
            </label>
            <div className="relative">
              <input
                type="text"
                id="title"
                name="title"
                defaultValue={initialTitle}
                placeholder="Поиск по названию..."
                className="w-full rounded-lg border border-[var(--border)] bg-[var(--bg)] px-3.5 py-2 text-sm text-[var(--text-h)] placeholder:text-[var(--text)]/60 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none"
              />
            </div>
          </div>

          {/* Дата от */}
          <div className="space-y-1">
            <label
              htmlFor="from"
              className="block text-xs font-semibold tracking-wider text-[var(--text)] uppercase"
            >
              Дата с
            </label>
            <input
              type="date"
              id="from"
              name="from"
              defaultValue={initialFrom}
              className="w-full rounded-lg border border-[var(--border)] bg-[var(--bg)] px-3.5 py-2 text-sm text-[var(--text-h)] focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none"
            />
          </div>

          {/* Дата по */}
          <div className="space-y-1">
            <label
              htmlFor="to"
              className="block text-xs font-semibold tracking-wider text-[var(--text)] uppercase"
            >
              Дата по
            </label>
            <input
              type="date"
              id="to"
              name="to"
              defaultValue={initialTo}
              className="w-full rounded-lg border border-[var(--border)] bg-[var(--bg)] px-3.5 py-2 text-sm text-[var(--text-h)] focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none"
            />
          </div>
        </div>

        {/* Кнопки действий */}
        <div className="flex flex-wrap items-center justify-end gap-3 pt-2">
          {hasActiveFilters && (
            <Link
              to="/"
              className="rounded-lg border border-[var(--border)] px-4 py-2 text-sm font-medium text-[var(--text)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              Сбросить
            </Link>
          )}

          <button
            type="submit"
            disabled={isSearching}
            className="flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700 disabled:opacity-50"
          >
            {isSearching ? (
              <>
                <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                </svg>
                <span>Поиск...</span>
              </>
            ) : (
              <span>Применить</span>
            )}
          </button>
        </div>
      </Form>
    </div>
  );
};
