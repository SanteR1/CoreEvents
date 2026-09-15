import { Form, Link, useNavigation } from 'react-router';
import { type BookingResponse } from '@/features/bookings/api/bookingsApi';

interface BookingDetailsProps {
  booking: BookingResponse;
  isCancellingInProgress?: boolean;
}

export const BookingDetails = ({ booking, isCancellingInProgress }: BookingDetailsProps) => {
  const navigation = useNavigation();
  const isCancelling =
    navigation.state === 'submitting' && navigation.formData?.get('intent') === 'cancel';

  const canCancel =
    (booking.status === 'Pending' || booking.status === 'Confirmed') && !isCancellingInProgress;

  const getStatusBadge = (status: BookingResponse['status']) => {
    if (isCancellingInProgress) {
      return (
        <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-800 dark:bg-amber-950/50 dark:text-amber-300">
          <span className="relative flex h-1.5 w-1.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-amber-400 opacity-75" />
            <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-amber-500" />
          </span>
          Отмена в процессе...
        </span>
      );
    }

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
            В обработке (Pending)
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

  return (
    <div className="space-y-6 rounded-2xl border border-(--border) bg-(--bg) p-6 text-left shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="rounded bg-(--code-bg) px-2.5 py-1 font-mono text-xs text-(--text)">
          ID бронирования: {booking.id}
        </span>

        {getStatusBadge(booking.status)}
      </div>

      <div className="space-y-4">
        <div className="grid grid-cols-1 gap-4 border-y border-(--border) py-4 text-sm sm:grid-cols-2">
          <div>
            <span className="text-xs font-medium text-(--text)">Событие:</span>
            <div className="mt-1">
              <Link
                to={`/events/${booking.eventId}`}
                className="font-medium text-indigo-600 hover:underline dark:text-indigo-400"
              >
                Открыть страницу события →
              </Link>
            </div>
          </div>

          <div>
            <span className="text-xs font-medium text-(--text)">Статус заявки:</span>
            <div className="mt-1">{getStatusBadge(booking.status)}</div>
          </div>

          <div>
            <span className="text-xs font-medium text-(--text)">Дата создания:</span>
            <p className="mt-1 font-medium text-(--text-h)">
              {booking.createdAt ? new Date(booking.createdAt).toLocaleString('ru-RU') : '—'}
            </p>
          </div>

          <div>
            <span className="text-xs font-medium text-(--text)">Дата обработки:</span>
            <p className="mt-1 font-medium text-(--text-h)">
              {booking.processedAt
                ? new Date(booking.processedAt).toLocaleString('ru-RU')
                : 'Ожидает обработки'}
            </p>
          </div>
        </div>

        {/* Секция действий */}
        <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
          <Link
            to="/"
            className="rounded-lg border border-(--border) px-4 py-2 text-xs font-medium text-(--text) transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            ← К афише событий
          </Link>

          {isCancellingInProgress && (
            <span className="inline-flex items-center gap-2 rounded-lg bg-amber-50 px-3.5 py-2 text-xs font-medium text-amber-700 dark:bg-amber-950/40 dark:text-amber-300">
              <span className="relative flex h-2 w-2">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-amber-400 opacity-75" />
                <span className="relative inline-flex h-2 w-2 rounded-full bg-amber-500" />
              </span>
              Отмена обрабатывается сервером...
            </span>
          )}

          {canCancel && (
            <Form
              method="post"
              onSubmit={(e) => {
                if (!window.confirm('Вы действительно хотите отменить это бронирование?')) {
                  e.preventDefault();
                }
              }}
            >
              <input type="hidden" name="intent" value="cancel" />
              <button
                type="submit"
                disabled={isCancelling}
                className="rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-xs font-semibold text-red-700 transition-colors hover:bg-red-100 disabled:opacity-50 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300 dark:hover:bg-red-900/50"
              >
                {isCancelling ? 'Отмена бронирования...' : 'Отменить бронирование'}
              </button>
            </Form>
          )}
        </div>
      </div>
    </div>
  );
};
