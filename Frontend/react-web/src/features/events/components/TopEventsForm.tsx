// src/features/events/components/TopEventsForm.tsx
import { type EventResponse } from '@/features/events/api/eventsApi';

interface TopEventsFormProps {
  events: EventResponse[];
  onBookClick?: (eventId: string) => void; // легко добавить новые пропсы позже
  isLoading?: boolean;
}

export const TopEventsForm = ({ events, onBookClick }: TopEventsFormProps) => {
  if (!events || events.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-(--border) p-8 text-center text-(--text)">
        В данный момент нет популярных событий.
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
      {events.map((event) => {
        const isSoldOut = event.availableSeats === 0;

        return (
          <div
            key={event.id}
            className="flex flex-col justify-between rounded-xl border border-(--border) bg-(--bg) p-5 shadow-sm transition-shadow hover:shadow-md"
          >
            <div>
              <h3 className="text-lg font-bold text-(--text-h)">
                {event.title ? event.title : 'Без названия'}
              </h3>
              <p className="mt-2 line-clamp-2 text-sm text-(--text)">
                {event.description ?? 'Описание отсутствует'}
              </p>
              <div className="mt-4 space-y-1 text-sm text-(--text)">
                <p>
                  <span className="font-medium text-(--text-h)">Начало: </span>
                  {event.startAt ? new Date(event.startAt).toLocaleString('ru-RU') : 'Не указано'}
                </p>
                <p>
                  <span className="font-medium text-(--text-h)">Окончание: </span>
                  {event.endAt ? new Date(event.endAt).toLocaleString('ru-RU') : 'Не указано'}
                </p>
                <p>
                  <span className="font-medium text-(--text-h)">Места: </span>
                  {event.availableSeats} / {event.totalSeats}
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={() => {
                if (onBookClick && event.id) {
                  onBookClick(event.id);
                }
              }}
              disabled={isSoldOut}
              className="mt-5 w-full rounded-lg bg-blue-600 px-6 py-2.5 font-medium text-white transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50 md:w-auto dark:bg-blue-500 dark:hover:bg-blue-600"
            >
              {isSoldOut ? 'Мест нет' : 'Забронировать'}
            </button>
          </div>
        );
      })}
    </div>
  );
};
