import { Link } from 'react-router';
import type { EventResponse } from '@/features/events/api/eventsApi';

interface EventCardProps {
  event: EventResponse;
}

function formatDate(date: Date): string {
  try {
    return new Date(date).toLocaleString('ru-RU', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return 'Дата не указана';
  }
}

export const EventCard = ({ event }: EventCardProps) => {
  const isSoldOut = event.availableSeats <= 0;
  const isFewSeats = !isSoldOut && event.availableSeats <= 5;

  return (
    <div className="flex flex-col justify-between rounded-xl border border-[var(--border)] bg-[var(--bg)] p-5 text-left shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md">
      <div className="space-y-3">
        {/* Статус мест */}
        <div className="flex items-center justify-between gap-2">
          {isSoldOut ? (
            <span className="inline-flex items-center rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-semibold text-red-800 dark:bg-red-950/50 dark:text-red-300">
              Мест нет
            </span>
          ) : isFewSeats ? (
            <span className="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-800 dark:bg-amber-950/50 dark:text-amber-300">
              Осталось мест: {event.availableSeats}
            </span>
          ) : (
            <span className="inline-flex items-center rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300">
              Свободно мест: {event.availableSeats} из {event.totalSeats}
            </span>
          )}

          <span className="text-xs text-[var(--text)]">Всего: {event.totalSeats}</span>
        </div>

        {/* Название */}
        <h3 className="text-lg leading-snug font-bold text-[var(--text-h)]">
          <Link
            to={`/events/${event.id}`}
            className="transition-colors hover:text-indigo-600 dark:hover:text-indigo-400"
          >
            {event.title ? event.title : 'Без названия'}
          </Link>
        </h3>

        {/* Описание */}
        <p className="line-clamp-2 text-sm text-[var(--text)]">
          {event.description ?? 'Описание отсутствует'}
        </p>

        {/* Даты */}
        <div className="space-y-1 rounded-lg bg-[var(--code-bg)] p-3 text-xs text-[var(--text)]">
          <div className="flex items-center justify-between">
            <span className="font-medium text-[var(--text-h)]">Начало:</span>
            <span>{formatDate(event.startAt)}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="font-medium text-[var(--text-h)]">Окончание:</span>
            <span>{formatDate(event.endAt)}</span>
          </div>
        </div>
      </div>

      {/* Кнопки действий */}
      <div className="mt-5 flex items-center gap-2 border-t border-[var(--border)] pt-2">
        <Link
          to={`/events/${event.id}`}
          className="flex-1 rounded-lg border border-[var(--border)] px-3 py-2 text-center text-xs font-medium text-[var(--text-h)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          Подробнее
        </Link>

        {isSoldOut ? (
          <button
            type="button"
            disabled
            className="flex-1 cursor-not-allowed rounded-lg bg-gray-200 px-3 py-2 text-center text-xs font-medium text-gray-500 dark:bg-gray-800 dark:text-gray-400"
          >
            Распродано
          </button>
        ) : (
          <Link
            to={`/bookings/create/${event.id}`}
            className="flex-1 rounded-lg bg-indigo-600 px-3 py-2 text-center text-xs font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700"
          >
            Забронировать
          </Link>
        )}
      </div>
    </div>
  );
};
