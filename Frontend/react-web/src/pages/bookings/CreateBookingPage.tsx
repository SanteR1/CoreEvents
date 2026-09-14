//
import { requireAuthLoader } from '@/app/routes/loaders';
import { createBooking } from '@/features/bookings/api/bookingsApi';
import { BookingCreateForm } from '@/features/bookings/components/BookingCreateForm';
import { getEventById } from '@/features/events/api/eventsApi';
import { toFormError } from '@/shared/api/errors';

import {
  useLoaderData,
  useActionData,
  useNavigation,
  type ActionFunctionArgs,
  type LoaderFunctionArgs,
  redirect,
} from 'react-router';

// 1. LOADER: загружает актуальное количество мест перед показом страницы
export async function loader(args: LoaderFunctionArgs) {
  const authRedirect = requireAuthLoader(args);
  if (authRedirect) {
    return authRedirect;
  }

  const eventId = args.params.eventId;
  if (!eventId) {
    throw new Response('Идентификатор события не указан', {
      status: 404,
      statusText: 'Not Found',
    });
  }

  const res = await getEventById(eventId);
  if (!res.success || !res.event) {
    if (res.httpStatus === 404) {
      throw new Response('Событие не найдено', {
        status: 404,
        statusText: 'Not Found',
      });
    }
    if (res.httpStatus === 403) {
      throw new Response('Доступ к событию ограничен', {
        status: 403,
        statusText: 'Forbidden',
      });
    }
    throw new Response(res.error?.message ?? 'Не удалось загрузить данные события', {
      status: res.httpStatus >= 400 ? res.httpStatus : 500,
    });
  }

  return { event: res.event };
}

// 2. Action функция роутера
export async function action({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const rawEventId = formData.get('eventId');
  const eventId = typeof rawEventId === 'string' ? rawEventId : '';

  const rawSeats = formData.get('seats');
  const seats = typeof rawSeats === 'string' ? rawSeats : '1';

  if (!eventId) {
    return { error: toFormError('Идентификатор события не найден') };
  }

  try {
    const res = await createBooking(eventId, seats);

    // 1. Ошибка от API или сети (res.error уже содержит текст)
    if (!res.success) {
      return {
        error: res.error,
      };
    }

    // 2. Защита: проверяем, что ID действительно вернулся
    const bookingId = res.booking?.id || res.statusUrl?.split('/').filter(Boolean).pop();
    if (!bookingId) {
      return {
        error: toFormError('Заявка принята, но не удалось получить номер бронирования'),
      };
    }

    // 3. Успешный редирект
    return redirect(`/bookings/${bookingId}`);
  } catch (err) {
    // ВАЖНО для React Router: если выброшен Response (например, redirect), не перехватываем его
    if (err instanceof Response) {
      throw err;
    }

    // Неожиданная критическая ошибка JS в рантайме
    return {
      error: toFormError(err, {
        defaultMessage: 'Не удалось оформить бронирование. Пожалуйста, попробуйте позже.',
      }),
    };
  }
}

export const CreateBookingPage = () => {
  const { event } = useLoaderData<typeof loader>();

  // Сюда прилетит ошибка из action
  // Указываем, что данные могут быть undefined (при первой загрузке)
  // или объектом с ошибкой (после неудачного сабмита)
  const actionResult = useActionData<typeof action>();

  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  return (
    <div className="mx-auto max-w-xl px-4 py-10">
      <h1 className="mb-6 text-2xl font-bold text-[var(--text-h)]">Бронирование билетов</h1>

      <BookingCreateForm
        eventId={event.id}
        availableSeats={event.availableSeats}
        isSubmitting={isSubmitting}
        error={actionResult?.error}
      />
    </div>
  );
};
