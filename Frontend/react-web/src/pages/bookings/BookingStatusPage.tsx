import { useEffect, useState } from 'react';
import {
  useLoaderData,
  useActionData,
  useRevalidator,
  type LoaderFunctionArgs,
  type ActionFunctionArgs,
} from 'react-router';
import { getBookingById, deleteBookingById } from '@/features/bookings/api/bookingsApi';
import { BookingDetails } from '@/features/bookings/components/BookingDetails';
import { toFormError } from '@/shared/api/errors';
import { requireAuthLoader } from '@/app/routes/loaders';

const CANCELLING_PREFIX = 'cancelling_booking_';

export async function loader(args: LoaderFunctionArgs) {
  const authRedirect = requireAuthLoader(args);
  if (authRedirect) {
    return authRedirect;
  }

  const { bookingId } = args.params;

  if (!bookingId) {
    throw new Response('Идентификатор бронирования не указан', {
      status: 404,
      statusText: 'Not Found',
    });
  }

  try {
    // Передаем signal из роутера для корректной отмены при уходе со страницы
    const res = await getBookingById(bookingId, { signal: args.request.signal });

    if (!res.success || !res.booking) {
      if (res.httpStatus === 404) {
        throw new Response('Бронирование не найдено', {
          status: 404,
          statusText: 'Not Found',
        });
      }
      if (res.httpStatus === 403) {
        throw new Response('У вас нет доступа к этому бронированию', {
          status: 403,
          statusText: 'Forbidden',
        });
      }
      throw new Response(res.error?.message ?? 'Не удалось загрузить бронирование', {
        status: res.httpStatus >= 400 ? res.httpStatus : 500,
      });
    }

    return { booking: res.booking };
  } catch (err) {
    if (err instanceof Response) {
      throw err;
    }
    if (err instanceof DOMException && err.name === 'AbortError') {
      throw err;
    }
    throw new Response('Ошибка соединения с сервисом бронирований', {
      status: 500,
      statusText: 'Internal Server Error',
    });
  }
}

export async function action(args: ActionFunctionArgs) {
  const authRedirect = requireAuthLoader(args);
  if (authRedirect) {
    return authRedirect;
  }

  const { bookingId } = args.params;
  if (!bookingId) {
    return { error: toFormError('Идентификатор бронирования не указан') };
  }

  const formData = await args.request.formData();
  const intent = formData.get('intent');

  if (intent === 'cancel') {
    const res = await deleteBookingById(bookingId);
    if (!res.success) {
      return { error: res.error };
    }
    return { success: true, message: 'Заявка на отмену бронирования принята' };
  }

  return null;
}

export const GetBookingPage = () => {
  const { booking } = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const revalidator = useRevalidator();

  const isPendingStatus = booking.status === 'Pending';

  // Проверяем флаг отмены в sessionStorage, чтобы не терять состояние при обновлении страницы (F5)
  const [isStoredCancelling] = useState(() => {
    if (typeof window === 'undefined') return false;
    return Boolean(sessionStorage.getItem(`${CANCELLING_PREFIX}${booking.id}`));
  });

  const isCancellingInProgress =
    (Boolean(actionData?.success) || isStoredCancelling) &&
    booking.status !== 'Cancelled' &&
    !actionData?.error;

  // Синхронизируем sessionStorage с актуальным состоянием
  useEffect(() => {
    if (actionData?.success && booking.status !== 'Cancelled') {
      sessionStorage.setItem(`${CANCELLING_PREFIX}${booking.id}`, 'true');
    }

    if (booking.status === 'Cancelled' || actionData?.error) {
      sessionStorage.removeItem(`${CANCELLING_PREFIX}${booking.id}`);
    }
  }, [booking.id, booking.status, actionData?.success, actionData?.error]);

  // Запускаем опрос, если бронь либо создается (Pending), либо в процессе отмены
  const shouldPoll = isPendingStatus || isCancellingInProgress;

  // Надежный рекурсивный поллинг через setTimeout, независимый от смены ссылок revalidator
  useEffect(() => {
    if (!shouldPoll) {
      return;
    }

    let isMounted = true;
    let timerId: ReturnType<typeof setTimeout>;

    const poll = async () => {
      if (!isMounted) return;

      try {
        if (revalidator.state === 'idle') {
          await revalidator.revalidate();
        }
      } catch {
        // Фоновые сетевые сбои игнорируются, следующий опрос продолжится
      } finally {
        if (isMounted) {
          timerId = setTimeout(poll, 2000);
        }
      }
    };

    timerId = setTimeout(poll, 2000);

    return () => {
      isMounted = false;
      clearTimeout(timerId);
    };
  }, [shouldPoll, revalidator]);

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6 text-left">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-(--text-h)">Статус бронирования</h1>
          <p className="text-sm text-(--text)">Просмотр и управление информацией о бронировании</p>
        </div>
        {(revalidator.state === 'loading' || shouldPoll) && (
          <span className="flex items-center gap-1.5 text-xs text-(--text)">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-indigo-400 opacity-75" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-indigo-500" />
            </span>
            {revalidator.state === 'loading' ? 'Обновление данных...' : 'Фоновое отслеживание...'}
          </span>
        )}
      </div>

      {actionData?.error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/20 dark:text-red-400">
          {actionData.error.message}
        </div>
      )}

      {actionData?.success && (
        <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 dark:border-green-900/50 dark:bg-green-950/20 dark:text-green-300">
          {actionData.message}
        </div>
      )}

      <BookingDetails booking={booking} isCancellingInProgress={isCancellingInProgress} />
    </div>
  );
};
