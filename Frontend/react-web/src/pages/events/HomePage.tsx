import { useLoaderData, Link, type LoaderFunctionArgs } from 'react-router';
import { getAllEvents } from '@/features/events/api/eventsApi';
import { EventFilters } from '@/features/events/components/EventFilters';
import { EventCard } from '@/features/events/components/EventCard';
import { EventPagination } from '@/features/events/components/EventPagination';

export async function loader({ request }: LoaderFunctionArgs) {
  const url = new URL(request.url);

  const title = url.searchParams.get('title')?.trim() ?? undefined;
  const rawFrom = url.searchParams.get('from')?.trim() ?? undefined;
  const rawTo = url.searchParams.get('to')?.trim() ?? undefined;

  const pageParam = parseInt(url.searchParams.get('page') ?? '1', 10);
  const page = isNaN(pageParam) || pageParam < 1 ? 1 : pageParam;

  const pageSizeParam = parseInt(url.searchParams.get('pageSize') ?? '6', 10);
  const pageSize = isNaN(pageSizeParam) || pageSizeParam < 1 ? 6 : pageSizeParam;

  // Форматируем даты в ISO-строку для бэкенда
  let from: string | undefined;
  let to: string | undefined;

  if (rawFrom) {
    const fromDate = new Date(`${rawFrom}T00:00:00`);
    if (!isNaN(fromDate.getTime())) {
      from = fromDate.toISOString();
    }
  }

  if (rawTo) {
    const toDate = new Date(`${rawTo}T23:59:59.999`);
    if (!isNaN(toDate.getTime())) {
      to = toDate.toISOString();
    }
  }

  const result = await getAllEvents(title, from, to, page, pageSize);

  return {
    result,
    filters: {
      title: title ?? '',
      from: rawFrom ?? '',
      to: rawTo ?? '',
      page,
      pageSize,
    },
  };
}

export const HomePage = () => {
  const { result, filters } = useLoaderData<typeof loader>();

  const isSuccess = result.success;
  const events = isSuccess ? (result.event.items.event ?? []) : [];
  const totalCount = isSuccess ? result.event.totalCount : 0;
  const totalPages = isSuccess ? result.event.totalPages : 1;
  const currentPage = isSuccess ? result.event.currentPage : 1;

  return (
    <div className="mx-auto max-w-6xl space-y-8 py-6">
      {/* Шапка каталога */}
      <div className="flex flex-col gap-4 text-left sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-[var(--text-h)] sm:text-4xl">
            Афиша событий
          </h1>
          <p className="mt-1 text-sm text-[var(--text)] sm:text-base">
            Актуальные события, встречи и конференции. Выбирайте и бронируйте билеты онлайн.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link
            to="/events/topevents"
            className="rounded-lg border border-[var(--border)] bg-[var(--bg)] px-4 py-2 text-sm font-medium text-[var(--text-h)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            🔥 Топ событий
          </Link>
          <Link
            to="/events/create"
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700"
          >
            + Создать событие
          </Link>
        </div>
      </div>

      {/* Панель фильтров */}
      <EventFilters
        initialTitle={filters.title}
        initialFrom={filters.from}
        initialTo={filters.to}
        pageSize={filters.pageSize}
      />

      {/* Ошибка загрузки */}
      {!isSuccess && (
        <div className="rounded-xl border border-red-200 bg-red-50/70 p-6 text-center text-red-700 dark:border-red-900/50 dark:bg-red-950/20 dark:text-red-300">
          <h3 className="text-base font-bold">Не удалось загрузить события</h3>
          <p className="mt-1 text-sm">
            {result.error.message ?? 'Пожалуйста, проверьте подключение и повторите попытку.'}
          </p>
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="mt-4 rounded-lg bg-red-600 px-4 py-2 text-xs font-semibold text-white transition-colors hover:bg-red-700"
          >
            Повторить попытку
          </button>
        </div>
      )}

      {/* Список событий */}
      {isSuccess && (
        <>
          {events.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-[var(--border)] p-12 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-gray-100 text-gray-500 dark:bg-gray-800">
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                  />
                </svg>
              </div>
              <h3 className="mt-3 text-lg font-semibold text-[var(--text-h)]">
                События не найдены
              </h3>
              <p className="mt-1 text-sm text-[var(--text)]">
                По заданным критериям ничего не найдено. Попробуйте изменить параметры поиска или
                сбросить фильтры.
              </p>
              {(filters.title || filters.from || filters.to) && (
                <Link
                  to="/"
                  className="mt-4 inline-block rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-indigo-700"
                >
                  Сбросить фильтры
                </Link>
              )}
            </div>
          ) : (
            <div className="space-y-6">
              {/* Сетка событий */}
              <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
                {events.map((event) => (
                  <EventCard key={event.id} event={event} />
                ))}
              </div>

              {/* Пагинация */}
              <EventPagination
                currentPage={currentPage}
                totalPages={totalPages}
                totalCount={totalCount}
                pageSize={filters.pageSize}
                filters={filters}
              />
            </div>
          )}
        </>
      )}
    </div>
  );
};
