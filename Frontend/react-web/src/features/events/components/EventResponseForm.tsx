// src/features/events/components/EventResponseForm.tsx
import { Link } from 'react-router';
import { type EventResponse } from '@/features/events/api/eventsApi';
import { formatPrice } from '@/shared/lib/format/price';

interface EventResponseFormProps {
  event: EventResponse;
  onCreateAnother?: () => void;
}

export const EventResponseForm = ({ event, onCreateAnother }: EventResponseFormProps) => {
  const isCancelled = !event.isActive;

  return (
    <div className="space-y-5 rounded-2xl border border-emerald-200 bg-emerald-50/50 p-6 text-left shadow-sm dark:border-emerald-900/40 dark:bg-emerald-950/20">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <span className="text-xl">🎉</span>
          <h2 className="text-lg font-bold text-(--text-h)">Событие успешно создано</h2>
        </div>
        <span className="rounded bg-emerald-100 px-2.5 py-1 font-mono text-xs text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-300">
          ID: {event.id}
        </span>
      </div>

      <div className="space-y-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-xl font-bold text-(--text-h)">{event.title}</h3>
            {isCancelled ? (
              <span className="rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-semibold text-red-800 dark:bg-red-950/50 dark:text-red-300">
                Отменено
              </span>
            ) : (
              <span className="rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300">
                Активно
              </span>
            )}
          </div>
          {event.description && (
            <p className="mt-2 text-sm leading-relaxed text-(--text)">{event.description}</p>
          )}
        </div>

        <div className="grid grid-cols-1 gap-3 rounded-xl border border-emerald-200/60 bg-white/60 p-4 text-xs text-(--text) sm:grid-cols-2 dark:border-emerald-900/30 dark:bg-(--code-bg)/60">
          <div>
            <span className="font-semibold text-(--text-h)">Дата и время начала:</span>
            <p className="mt-0.5 font-medium">
              {event.startAt ? new Date(event.startAt).toLocaleString('ru-RU') : '—'}
            </p>
          </div>
          <div>
            <span className="font-semibold text-(--text-h)">Дата и время окончания:</span>
            <p className="mt-0.5 font-medium">
              {event.endAt ? new Date(event.endAt).toLocaleString('ru-RU') : '—'}
            </p>
          </div>
          <div>
            <span className="font-semibold text-(--text-h)">Всего мест:</span>
            <p className="mt-0.5 font-medium">{event.totalSeats}</p>
          </div>
          <div>
            <span className="font-semibold text-(--text-h)">Доступно мест:</span>
            <p className="mt-0.5 font-bold text-indigo-600 dark:text-indigo-400">
              {event.availableSeats}
            </p>
          </div>
          <div className="flex items-center justify-between border-t border-emerald-200/50 pt-2 sm:col-span-2 dark:border-emerald-900/30">
            <span className="font-semibold text-(--text-h)">Стоимость билета:</span>
            <span className="text-sm font-bold text-indigo-600 dark:text-indigo-400">
              {formatPrice(event.price, event.currency)}
            </span>
          </div>
        </div>
      </div>

      {/* Быстрые действия */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-emerald-200/60 pt-4 dark:border-emerald-900/30">
        <div className="flex flex-wrap items-center gap-3">
          <Link
            to={`/events/${event.id}`}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-xs font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700"
          >
            Перейти к событию →
          </Link>

          {onCreateAnother ? (
            <button
              type="button"
              onClick={onCreateAnother}
              className="rounded-lg border border-(--border) bg-(--bg) px-4 py-2 text-xs font-medium text-(--text-h) transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              + Создать ещё одно
            </button>
          ) : (
            <Link
              to="/events/create"
              className="rounded-lg border border-(--border) bg-(--bg) px-4 py-2 text-xs font-medium text-(--text-h) transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
            >
              + Создать ещё одно
            </Link>
          )}
        </div>

        <Link
          to="/"
          className="text-xs font-medium text-(--text) transition-colors hover:text-(--text-h)"
        >
          К афише событий
        </Link>
      </div>
    </div>
  );
};
