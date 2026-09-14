import { useState } from 'react';
import { Form, Link } from 'react-router';
import { type FormActionError } from '@/shared/api/errors';

interface BookingCreateFormProps {
  eventId: string;
  isSubmitting: boolean;
  error?: FormActionError | null;
  availableSeats: number;
}

export const BookingCreateForm = ({
  eventId,
  availableSeats,
  isSubmitting,
  error,
}: BookingCreateFormProps) => {
  // Позволяем состоянию быть пустой строкой для комфортного редактирования
  const [seats, setSeats] = useState<number | ''>(1);

  // 2. НЕТ МЕСТ
  if (availableSeats <= 0) {
    return (
      <div className="space-y-4 rounded-xl border border-(--border) bg-(--bg) p-8 text-center shadow-sm">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400">
          <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M15 5v2m0 4v2m0 4v2M5 5a2 2 0 00-2 2v3a2 2 0 110 4v3a2 2 0 002 2h14a2 2 0 002-2v-3a2 2 0 110-4V7a2 2 0 00-2-2H5z"
            />
          </svg>
        </div>
        <div>
          <h3 className="text-lg font-semibold text-(--text-h)">Все места распроданы</h3>
          <p className="mt-1 text-sm text-(--text)">
            К сожалению, на это событие больше нет свободных мест.
          </p>
        </div>
        <Link
          to="/events/topevents"
          className="inline-block rounded-lg bg-(--accent) px-5 py-2 text-sm font-medium text-white transition-opacity hover:opacity-90"
        >
          Посмотреть другие события
        </Link>
      </div>
    );
  }

  // 3. ФОРМА ВВОДА
  const currentSeats = seats === '' ? 1 : seats;

  const handleDecrement = () => {
    setSeats((prev) => Math.max(1, (typeof prev === 'number' ? prev : 1) - 1));
  };

  const handleIncrement = () => {
    setSeats((prev) => Math.min(availableSeats, (typeof prev === 'number' ? prev : 1) + 1));
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    if (value === '') {
      setSeats(''); // Позволяем инпуту быть временно пустым
      return;
    }

    const parsed = parseInt(value, 10);
    if (!isNaN(parsed)) {
      setSeats(Math.min(Math.max(1, parsed), availableSeats));
    }
  };

  // Возвращаем фокус на 1, если пользователь оставил поле пустым и нажал
  const handleBlur = () => {
    if (seats === '') setSeats(1);
  };

  return (
    <Form
      method="post"
      className="space-y-6 rounded-2xl border border-(--border) bg-(--bg) p-6 shadow-sm"
    >
      {error && (
        <div
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300"
        >
          <div className="flex items-start gap-3">
            <span className="text-base">⚠️</span>
            <div className="flex-1">
              <p className="font-semibold">Не удалось оформить бронирование</p>
              <p className="mt-1 text-xs">{error.message}</p>
              <p className="mt-2 text-xs text-(--text)">
                Вы можете нажать кнопку ниже, чтобы повторить отправку заявки, или вернуться назад к
                событию.
              </p>
            </div>
          </div>
        </div>
      )}

      <input type="hidden" name="eventId" value={eventId} />

      <div className="space-y-2">
        <label htmlFor="seats" className="block text-sm font-medium text-(--text)">
          Количество мест
        </label>
        <div className="flex items-center justify-center space-x-3">
          <button
            type="button"
            aria-label="Уменьшить количество мест"
            onClick={handleDecrement}
            disabled={currentSeats <= 1 || isSubmitting || availableSeats <= 0}
            className="flex h-10 w-10 items-center justify-center rounded-lg border border-(--border) bg-(--bg) text-lg font-bold text-(--text-h) hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:hover:bg-gray-800"
          >
            -
          </button>

          <input
            type="number"
            id="seats"
            name="seats"
            min={1}
            max={availableSeats}
            value={seats}
            onChange={handleInputChange}
            onBlur={handleBlur}
            disabled={isSubmitting}
            className="h-10 w-20 rounded-lg border border-(--border) bg-(--bg) text-center font-semibold text-(--text-h) focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none"
          ></input>

          <button
            type="button"
            aria-label="Увеличить количество мест"
            onClick={handleIncrement}
            disabled={currentSeats >= availableSeats || isSubmitting}
            className="flex h-10 w-10 items-center justify-center rounded-lg border border-(--border) bg-(--bg) text-lg font-bold text-(--text-h) hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:hover:bg-gray-800"
          >
            +
          </button>
        </div>
        <p className="text-xs text-(--text) opacity-75">
          Доступно для выбора: до {availableSeats} мест
        </p>
      </div>

      <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:items-center sm:justify-between">
        <Link
          to={`/events/${eventId}`}
          className="rounded-xl border border-(--border) px-4 py-2.5 text-center text-sm font-medium text-(--text) transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          ← Назад к событию
        </Link>

        <button
          type="submit"
          disabled={isSubmitting || availableSeats <= 0}
          className="flex-1 rounded-xl bg-indigo-600 px-5 py-2.5 text-center text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-500 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSubmitting ? 'Отправка заявки...' : `Забронировать (${currentSeats})`}
        </button>
      </div>
    </Form>
  );
};
