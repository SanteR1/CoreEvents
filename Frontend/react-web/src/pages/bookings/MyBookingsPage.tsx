import { useLoaderData, Link, useSearchParams, type LoaderFunctionArgs } from 'react-router';
import { requireAuthLoader } from '@/shared/lib/auth';
import { getMyBookings, type BookingResponse } from '@/features/bookings/api/bookingsApi';
import { formatPrice } from '@/shared/lib/format/price';
import type { BookingSchema } from '@/shared/api';

type BookingStatus = BookingSchema<'BookingStatus'>;

export async function loader(args: LoaderFunctionArgs) {
  const authRedirect = await requireAuthLoader(args);
  if (authRedirect) {
    return authRedirect;
  }

  const url = new URL(args.request.url);
  const statusParam = url.searchParams.get('status')?.trim();
  const pageParam = parseInt(url.searchParams.get('page') ?? '1', 10);
  const page = isNaN(pageParam) || pageParam < 1 ? 1 : pageParam;

  const validStatuses: BookingStatus[] = [
    'Pending',
    'Confirmed',
    'Rejected',
    'Cancelled',
    'CancellationPending',
  ];
  const status = validStatuses.includes(statusParam as BookingStatus)
    ? (statusParam as BookingStatus)
    : undefined;

  const res = await getMyBookings({
    status,
    page,
    pageSize: 10,
  });

  return {
    res,
    currentStatus: statusParam ?? '',
    page,
  };
}

export const MyBookingsPage = () => {
  const { res, currentStatus, page } = useLoaderData<typeof loader>();
  const [searchParams, setSearchParams] = useSearchParams();

  const isSuccess = res.success;
  const bookings: BookingResponse[] = isSuccess ? res.result.items : [];
  const totalCount = isSuccess ? res.result.totalCount : 0;
  const totalPages = isSuccess ? res.result.totalPages : 1;

  const handleStatusFilter = (status: string) => {
    const next = new URLSearchParams(searchParams);
    if (status) {
      next.set('status', status);
    } else {
      next.delete('status');
    }
    next.set('page', '1');
    setSearchParams(next);
  };

  const handlePageChange = (newPage: number) => {
    const next = new URLSearchParams(searchParams);
    next.set('page', newPage.toString());
    setSearchParams(next);
  };

  const getStatusBadge = (status: BookingResponse['status']) => {
    switch (status) {
      case 'Confirmed':
        return (
          <span className="inline-flex items-center rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300">
            Подтверждено
          </span>
        );
      case 'Pending':
        return (
          <span className="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-800 dark:bg-amber-950/50 dark:text-amber-300">
            В обработке
          </span>
        );
      case 'CancellationPending':
        return (
          <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-800 dark:bg-amber-950/50 dark:text-amber-300">
            Отмена...
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-semibold text-gray-700 dark:bg-gray-800 dark:text-gray-300">
            Отменено
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-semibold text-red-800 dark:bg-red-950/50 dark:text-red-300">
            Отклонено
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-semibold text-gray-800">
            {status}
          </span>
        );
    }
  };

  const filterTabs = [
    { label: 'Все', value: '' },
    { label: 'В обработке', value: 'Pending' },
    { label: 'Подтвержденные', value: 'Confirmed' },
    { label: 'Отмененные', value: 'Cancelled' },
  ];

  return (
    <div className="mx-auto max-w-5xl space-y-6 py-6 text-left">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight text-(--text-h)">
            Мои бронирования
          </h1>
          <p className="mt-1 text-sm text-(--text)">
            История оформленных билетов и управление текущими бронями
          </p>
        </div>
      </div>

      {/* Вкладки фильтрации */}
      <div className="flex flex-wrap gap-2 border-b border-(--border) pb-3">
        {filterTabs.map((tab) => {
          const isActive = currentStatus === tab.value;
          return (
            <button
              key={tab.value}
              type="button"
              onClick={() => handleStatusFilter(tab.value)}
              className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-indigo-600 text-white shadow-sm'
                  : 'border border-(--border) bg-(--bg) text-(--text) hover:bg-gray-100 dark:hover:bg-gray-800'
              }`}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Список или пустое состояние */}
      {!isSuccess ? (
        <div
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 p-6 text-center text-red-700 dark:border-red-900/50 dark:bg-red-950/20 dark:text-red-400"
        >
          <p className="font-semibold">{res.error.message}</p>
        </div>
      ) : bookings.length === 0 ? (
        <div className="rounded-xl border border-dashed border-(--border) p-12 text-center">
          <p className="text-base font-medium text-(--text-h)">Бронирований не найдено</p>
          <p className="mt-1 text-sm text-(--text)">
            У вас пока нет оформленных заявок с выбранным фильтром
          </p>
          <div className="mt-4">
            <Link
              to="/"
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700"
            >
              Перейти к афише событий
            </Link>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          <div className="overflow-hidden rounded-xl border border-(--border) bg-(--bg) shadow-sm">
            <table className="w-full text-left text-sm">
              <thead className="bg-(--code-bg) text-xs font-semibold text-(--text) uppercase">
                <tr>
                  <th className="px-4 py-3">Бронирование</th>
                  <th className="px-4 py-3">Места</th>
                  <th className="px-4 py-3">Стоимость</th>
                  <th className="px-4 py-3">Статус</th>
                  <th className="px-4 py-3">Создано</th>
                  <th className="px-4 py-3 text-right">Действие</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-(--border)">
                {bookings.map((booking) => (
                  <tr key={booking.id} className="transition-colors hover:bg-(--code-bg)/40">
                    <td className="px-4 py-3 font-mono text-xs">
                      <Link
                        to={`/bookings/${booking.id}`}
                        className="font-semibold text-indigo-600 hover:underline dark:text-indigo-400"
                      >
                        {booking.id.slice(0, 8)}...
                      </Link>
                    </td>
                    <td className="px-4 py-3 font-medium">{booking.seats}</td>
                    <td className="px-4 py-3 font-semibold text-(--text-h)">
                      {formatPrice(booking.totalPrice, booking.currency)}
                    </td>
                    <td className="px-4 py-3">{getStatusBadge(booking.status)}</td>
                    <td className="px-4 py-3 text-xs text-(--text)">
                      {booking.createdAt
                        ? new Date(booking.createdAt).toLocaleString('ru-RU')
                        : '—'}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        to={`/bookings/${booking.id}`}
                        className="rounded-lg border border-(--border) px-3 py-1.5 text-xs font-medium text-(--text-h) transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
                      >
                        Подробнее →
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Пагинация */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <span className="text-xs text-(--text)">
                Всего: {totalCount} бронирований (Страница {page} из {totalPages})
              </span>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => handlePageChange(page - 1)}
                  className="rounded-lg border border-(--border) bg-(--bg) px-3 py-1.5 text-xs font-medium text-(--text-h) hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:hover:bg-gray-800"
                >
                  ← Назад
                </button>
                <button
                  type="button"
                  disabled={page >= totalPages}
                  onClick={() => handlePageChange(page + 1)}
                  className="rounded-lg border border-(--border) bg-(--bg) px-3 py-1.5 text-xs font-medium text-(--text-h) hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:hover:bg-gray-800"
                >
                  Вперед →
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
