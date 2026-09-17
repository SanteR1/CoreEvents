// src/features/events/components/EventResponseForm.tsx
import { type EventResponse } from '@/features/events/api/eventsApi';

interface EventResponseFormProps {
  event: EventResponse;
}

export const EventResponseForm = ({ event }: EventResponseFormProps) => {
  return (
    <div className="space-y-4 rounded-xl border border-green-200 bg-green-50/40 p-6">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-(--text-h)">Событие успешно создано</h2>
        <span className="rounded bg-green-100 px-2 py-1 font-mono text-xs text-green-800">
          ID: {event.id}
        </span>
      </div>

      <div className="space-y-2 text-sm text-(--text-h)">
        <p className="text-base font-bold text-gray-900">{event.title}</p>
        {event.description && <p className="text-gray-600">{event.description}</p>}

        <div className="grid grid-cols-1 gap-2 border-t border-green-200/60 pt-2 text-xs md:grid-cols-2">
          <div>
            <span className="text-(--text-h)">Начало:</span>{' '}
            {event.startAt ? new Date(event.startAt).toLocaleString('ru-RU') : '—'}
          </div>
          <div>
            <span className="text-(--text-h)">Окончание:</span>{' '}
            {event.endAt ? new Date(event.endAt).toLocaleString('ru-RU') : '—'}
          </div>
          <div>
            <span className="text-(--text-h)">Всего мест:</span> {event.totalSeats}
          </div>
          <div>
            <span className="text-(--text-h)">Доступно мест:</span>{' '}
            <span className="font-semibold text-(--text-h)">{event.availableSeats}</span>
          </div>
        </div>
      </div>
    </div>
  );
};
