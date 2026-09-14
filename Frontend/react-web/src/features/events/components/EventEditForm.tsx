import { Form, Link } from 'react-router';
import type { EventResponse } from '@/features/events/api/eventsApi';
import type { FormActionError } from '@/shared/api/errors';

interface EventEditFormProps {
  event: EventResponse;
  error?: FormActionError | null;
  isSubmitting: boolean;
}

function toDateTimeLocalString(date: Date | string): string {
  const d = new Date(date);
  if (isNaN(d.getTime())) return '';
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export const EventEditForm = ({ event, error, isSubmitting }: EventEditFormProps) => {
  return (
    <Form
      method="post"
      className="space-y-5 rounded-xl border border-[var(--border)] bg-[var(--bg)] p-6 text-left shadow-sm"
    >
      {/* Общее сообщение об ошибке */}
      {error?.message && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-3.5 text-sm text-red-600 dark:border-red-900/50 dark:bg-red-900/20 dark:text-red-400">
          {error.message}
        </div>
      )}

      {/* Поле Название */}
      <div>
        <label htmlFor="title" className="mb-1 block text-sm font-medium text-[var(--text-h)]">
          Название события <span className="text-red-500">*</span>
        </label>
        <input
          id="title"
          type="text"
          name="title"
          defaultValue={event.title}
          required
          placeholder="Например: Архитектурный митап"
          className={`w-full rounded-lg border bg-[var(--code-bg)] px-3.5 py-2 text-sm text-[var(--text-h)] transition-colors outline-none placeholder:text-[var(--text)] focus:ring-1 ${
            error?.fieldErrors?.title
              ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50'
              : 'border-[var(--border)] focus:border-[var(--accent)] focus:ring-[var(--accent)]'
          }`}
        />
        {error?.fieldErrors?.title && (
          <p className="mt-1 text-xs text-red-500 dark:text-red-400">
            {error.fieldErrors.title.join(' ')}
          </p>
        )}
      </div>

      {/* Даты начала и окончания */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="startAt" className="mb-1 block text-sm font-medium text-[var(--text-h)]">
            Дата и время начала <span className="text-red-500">*</span>
          </label>
          <input
            id="startAt"
            type="datetime-local"
            name="startAt"
            defaultValue={toDateTimeLocalString(event.startAt)}
            required
            className={`w-full rounded-lg border bg-[var(--code-bg)] px-3.5 py-2 text-sm text-[var(--text-h)] transition-colors outline-none focus:ring-1 dark:scheme-dark [&::-webkit-calendar-picker-indicator]:cursor-pointer ${
              error?.fieldErrors?.startAt
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50'
                : 'border-[var(--border)] focus:border-[var(--accent)] focus:ring-[var(--accent)]'
            }`}
          />
          {error?.fieldErrors?.startAt && (
            <p className="mt-1 text-xs text-red-500 dark:text-red-400">
              {error.fieldErrors.startAt.join(' ')}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="endAt" className="mb-1 block text-sm font-medium text-[var(--text-h)]">
            Дата и время окончания <span className="text-red-500">*</span>
          </label>
          <input
            id="endAt"
            type="datetime-local"
            name="endAt"
            defaultValue={toDateTimeLocalString(event.endAt)}
            required
            className={`w-full rounded-lg border bg-[var(--code-bg)] px-3.5 py-2 text-sm text-[var(--text-h)] transition-colors outline-none focus:ring-1 dark:scheme-dark [&::-webkit-calendar-picker-indicator]:cursor-pointer ${
              error?.fieldErrors?.endAt
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50'
                : 'border-[var(--border)] focus:border-[var(--accent)] focus:ring-[var(--accent)]'
            }`}
          />
          {error?.fieldErrors?.endAt && (
            <p className="mt-1 text-xs text-red-500 dark:text-red-400">
              {error.fieldErrors.endAt.join(' ')}
            </p>
          )}
        </div>
      </div>

      {/* Описание */}
      <div>
        <label
          htmlFor="description"
          className="mb-1 block text-sm font-medium text-[var(--text-h)]"
        >
          Описание события
        </label>
        <textarea
          id="description"
          name="description"
          rows={5}
          defaultValue={event.description ?? ''}
          placeholder="Подробное описание программы события..."
          className={`w-full resize-y rounded-lg border bg-[var(--code-bg)] px-3.5 py-2 text-sm text-[var(--text-h)] transition-colors outline-none placeholder:text-[var(--text)] focus:ring-1 ${
            error?.fieldErrors?.description
              ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50'
              : 'border-[var(--border)] focus:border-[var(--accent)] focus:ring-[var(--accent)]'
          }`}
        />
        {error?.fieldErrors?.description && (
          <p className="mt-1 text-xs text-red-500 dark:text-red-400">
            {error.fieldErrors.description.join(' ')}
          </p>
        )}
      </div>

      {/* Информационный блок о местах (только чтение) */}
      <div className="rounded-lg bg-[var(--code-bg)] p-3 text-xs text-[var(--text)]">
        <p>
          Количество мест (<strong className="text-[var(--text-h)]">{event.totalSeats}</strong>,
          доступно: <strong className="text-[var(--text-h)]">{event.availableSeats}</strong>)
          задается при создании события и не может быть изменено через эту форму.
        </p>
      </div>

      {/* Кнопки действий */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--border)] pt-4">
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex items-center gap-2 rounded-lg bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700 disabled:opacity-50"
          >
            {isSubmitting ? (
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
                <span>Сохранение...</span>
              </>
            ) : (
              <span>Сохранить изменения</span>
            )}
          </button>

          <Link
            to={`/events/${event.id}`}
            className="rounded-lg border border-[var(--border)] px-5 py-2.5 text-sm font-medium text-[var(--text)] transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            Отмена
          </Link>
        </div>

        {/* Кнопка удаления события */}
        <button
          type="submit"
          name="intent"
          value="delete"
          formNoValidate
          onClick={(e) => {
            if (
              !window.confirm(
                'Вы действительно хотите удалить это событие? Это действие необратимо.',
              )
            ) {
              e.preventDefault();
            }
          }}
          disabled={isSubmitting}
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-2.5 text-sm font-medium text-red-700 transition-colors hover:bg-red-100 disabled:opacity-50 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300 dark:hover:bg-red-900/50"
        >
          🗑️ Удалить событие
        </button>
      </div>
    </Form>
  );
};
