// src/features/events/components/EventRequestForm.tsx
import { Form } from 'react-router';
import { type FormActionError } from '@/shared/api/errors';

interface EventRequestFormProps {
  error?: FormActionError | null;
  isSubmitting: boolean;
}

export const EventRequestForm = ({ error, isSubmitting }: EventRequestFormProps) => {
  return (
    <Form
      method="post"
      className="space-y-4 rounded-xl border border-(--border) bg-(--bg) p-6 shadow-sm"
    >
      {/* 1. Общее сообщение об ошибке (сеть, 500, общий сбой) */}
      {error?.message && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-600 dark:border-red-900/50 dark:bg-red-900/20 dark:text-red-400">
          {error.message}
        </div>
      )}

      {/* Поле Название */}
      <div>
        <label htmlFor="title" className="mb-1 block text-sm font-medium text-(--text-h)">
          Название события
        </label>
        <input
          id="title"
          type="text"
          name="title"
          required
          placeholder="Например: Архитектурный митап"
          className={`w-full rounded-lg border border-(--border) bg-(--code-bg) px-3 py-2 text-(--text-h) transition-colors outline-none placeholder:text-(--text) focus:border-(--accent) focus:ring-1 ${
            error?.fieldErrors?.title
              ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50'
              : 'border-(--border) focus:border-(--accent) focus:ring-(--accent)'
          }`}
        />
        {/* Текст ошибки под title */}
        {error?.fieldErrors?.title && (
          <p className="mt-1 text-sm text-red-500 dark:text-red-400">
            {error.fieldErrors.title.join(' ')}
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div>
          <label htmlFor="startAt" className="mb-1 block text-sm font-medium text-(--text-h)">
            Начало
          </label>
          <input
            id="startAt"
            type="datetime-local"
            name="startAt"
            required
            className={`dark:scheme:dark w-full rounded-lg border bg-(--code-bg) px-3 py-2 text-(--text-h) transition-colors outline-none focus:ring-1 [&::-webkit-calendar-picker-indicator]:cursor-pointer ${
              error?.fieldErrors?.startAt
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50' // Стили при ошибке
                : 'border-(--border) focus:border-(--accent) focus:ring-(--accent)' // Обычные стили
            }`}
          />
          {/* Блок с текстом ошибки под полем */}
          {error?.fieldErrors?.startAt && (
            <p className="mt-1 text-sm text-red-500 dark:text-red-400">
              {error.fieldErrors.startAt.join(' ')}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="endAt" className="mb-1 block text-sm font-medium text-(--text-h)">
            Окончание
          </label>
          <input
            id="endAt"
            type="datetime-local"
            name="endAt"
            required
            className={`dark:scheme:dark w-full rounded-lg border bg-(--code-bg) px-3 py-2 text-(--text-h) transition-colors outline-none focus:ring-1 [&::-webkit-calendar-picker-indicator]:cursor-pointer ${
              error?.fieldErrors?.endAt
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50' // Стили при ошибке
                : 'border-(--border) focus:border-(--accent) focus:ring-(--accent)' // Обычные стили
            }`}
          />
          {/* Блок с текстом ошибки под полем */}
          {error?.fieldErrors?.endAt && (
            <p className="mt-1 text-sm text-red-500 dark:text-red-400">
              {error.fieldErrors.endAt.join(' ')}
            </p>
          )}
        </div>
      </div>

      {/* Поле Количество мест */}
      <div>
        <label htmlFor="totalSeats" className="mb-1 block text-sm font-medium text-(--text-h)">
          Количество мест
        </label>
        <input
          id="totalSeats"
          type="number"
          name="totalSeats"
          min="1"
          defaultValue={50}
          required
          className={`w-full rounded-lg border border-(--border) bg-(--code-bg) px-3 py-2 text-(--text-h) transition-colors outline-none focus:border-(--accent) focus:ring-1 focus:ring-(--accent) ${
            error?.fieldErrors?.totalSeats
              ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50' // Стили при ошибке
              : 'border-(--border) focus:border-(--accent) focus:ring-(--accent)' // Обычные стили
          }`}
        />
        {/* Текст ошибки под totalSeats */}
        {error?.fieldErrors?.totalSeats && (
          <p className="mt-1 text-sm text-red-500 dark:text-red-400">
            {error.fieldErrors.totalSeats.join(' ')}
          </p>
        )}
      </div>

      <div>
        <label htmlFor="description" className="mb-1 block text-sm font-medium text-(--text-h)">
          Описание
        </label>
        <textarea
          id="description"
          name="description"
          rows={4}
          placeholder="Краткое описание программы события..."
          className={`w-full resize-y rounded-lg border bg-(--code-bg) px-3 py-2 text-(--text-h) transition-colors outline-none placeholder:text-(--text) focus:ring-1 ${
            error?.fieldErrors?.description
              ? 'border-red-500 focus:border-red-500 focus:ring-red-500 dark:border-red-500/50'
              : 'border-(--border) focus:border-(--accent) focus:ring-(--accent)'
          }`}
        />
        {/* Текст ошибки под description */}
        {error?.fieldErrors?.description && (
          <p className="mt-1 text-sm text-red-500 dark:text-red-400">
            {error.fieldErrors.description.join(' ')}
          </p>
        )}
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full rounded-lg bg-(--accent) px-6 py-2.5 font-medium text-white transition-opacity hover:opacity-90 disabled:opacity-50 md:w-auto"
      >
        {isSubmitting ? 'Создание...' : 'Создать событие'}
      </button>
    </Form>
  );
};
