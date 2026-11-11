import {
  useLoaderData,
  useActionData,
  useNavigation,
  Link,
  Form,
  redirect,
  type LoaderFunctionArgs,
  type ActionFunctionArgs,
} from 'react-router';
import {
  getEventById,
  deleteEventById,
  cancelEventById,
  getEventReservations,
  type SeatReservationResponse,
} from '@/features/events/api/eventsApi';
import { requireAdminLoader } from '@/shared/lib/auth';
import { isAdmin as checkIsAdmin } from '@/shared/lib/auth/sessionStore';
import { toFormError } from '@/shared/api/errors';
import { useAuth } from '@/features/auth/hooks/useAuth';
import { formatPrice } from '@/shared/lib/format/price';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    throw new Response('Идентификатор события не указан', {
      status: 404,
      statusText: 'Not Found',
    });
  }

  const res = await getEventById(id);
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
    throw new Response(res.error?.message ?? 'Не удалось загрузить событие', {
      status: res.httpStatus >= 400 ? res.httpStatus : 500,
    });
  }

  let reservations: SeatReservationResponse[] = [];
  if (checkIsAdmin()) {
    try {
      const resv = await getEventReservations(id);
      if (resv.success) {
        reservations = resv.reservations;
      }
    } catch {
      // Игнорируем ошибки при загрузке резерваций
    }
  }

  return { event: res.event, reservations };
}

export async function action({ params, request }: ActionFunctionArgs) {
  const authRedirect = await requireAdminLoader({ request });
  if (authRedirect) {
    return authRedirect;
  }

  const { id } = params;
  if (!id) {
    return { error: toFormError('Идентификатор события не указан') };
  }

  const formData = await request.formData();
  const intent = formData.get('intent');

  if (intent === 'delete') {
    const res = await deleteEventById(id);
    if (!res.success) {
      return { error: res.error };
    }
    return redirect('/');
  }

  if (intent === 'cancel') {
    const res = await cancelEventById(id);
    if (!res.success) {
      return { error: res.error };
    }
    return { success: true, message: 'Событие успешно отменено' };
  }

  return null;
}

export const GetEventByIdPage = () => {
  const { event, reservations } = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const { isAdmin } = useAuth();
  const isDeleting =
    navigation.state === 'submitting' && navigation.formData?.get('intent') === 'delete';
  const isCancelling =
    navigation.state === 'submitting' && navigation.formData?.get('intent') === 'cancel';

  const isCancelled = !event.isActive;
  const isSoldOut = event.availableSeats <= 0;
  const isFewSeats = !isSoldOut && event.availableSeats <= 5;

  return (
    <div className="mx-auto max-w-3xl space-y-8 p-6 text-left">
      <Link
        to="/"
        className="inline-flex items-center gap-1 text-sm font-medium text-(--text) transition-colors hover:text-(--text-h)"
      >
        ← Назад к афише
      </Link>

      {/* Ошибка при удалении/отмене события */}
      {actionData?.error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/20 dark:text-red-400">
          {actionData.error.message}
        </div>
      )}

      {/* Успешное действие */}
      {actionData?.success && actionData?.message && (
        <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-700 dark:border-green-900/50 dark:bg-green-950/20 dark:text-green-300">
          {actionData.message}
        </div>
      )}

      <div className="space-y-6 rounded-2xl border border-(--border) bg-(--bg) p-8 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <span className="rounded-md bg-(--code-bg) px-2.5 py-1 font-mono text-xs text-(--text)">
            ID: {event.id}
          </span>

          <div className="flex items-center gap-2">
            {isCancelled ? (
              <span className="rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-800 dark:bg-red-950/50 dark:text-red-300">
                Событие отменено
              </span>
            ) : isSoldOut ? (
              <span className="rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-800 dark:bg-red-950/50 dark:text-red-300">
                Мест нет
              </span>
            ) : isFewSeats ? (
              <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800 dark:bg-amber-950/50 dark:text-amber-300">
                Осталось мест: {event.availableSeats}
              </span>
            ) : (
              <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300">
                Свободно мест: {event.availableSeats} из {event.totalSeats}
              </span>
            )}
          </div>
        </div>

        <div>
          <h1 className="text-2xl font-extrabold text-(--text-h) sm:text-3xl">{event.title}</h1>
          {event.description ? (
            <p className="mt-4 text-base leading-relaxed whitespace-pre-line text-(--text)">
              {event.description}
            </p>
          ) : (
            <p className="mt-4 text-sm text-(--text) italic">Описание не указано</p>
          )}
        </div>

        <div className="grid grid-cols-1 gap-4 rounded-xl bg-(--code-bg) p-4 text-sm text-(--text) sm:grid-cols-2">
          <div>
            <span className="font-semibold text-(--text-h)">Дата и время начала:</span>
            <p className="mt-0.5 font-medium">{new Date(event.startAt).toLocaleString('ru-RU')}</p>
          </div>
          <div>
            <span className="font-semibold text-(--text-h)">Дата и время окончания:</span>
            <p className="mt-0.5 font-medium">{new Date(event.endAt).toLocaleString('ru-RU')}</p>
          </div>
          <div>
            <span className="font-semibold text-(--text-h)">Всего мест:</span>
            <p className="mt-0.5 font-medium">{event.totalSeats}</p>
          </div>
          <div>
            <span className="font-semibold text-(--text-h)">Доступно для бронирования:</span>
            <p className="mt-0.5 font-bold text-indigo-600 dark:text-indigo-400">
              {event.availableSeats}
            </p>
          </div>
          <div className="flex items-center justify-between border-t border-(--border)/50 pt-2 sm:col-span-2">
            <span className="font-semibold text-(--text-h)">Стоимость билета:</span>
            <span className="text-base font-bold text-indigo-600 dark:text-indigo-400">
              {formatPrice(event.price, event.currency)}
            </span>
          </div>
        </div>

        {isCancelled && (
          <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/20 dark:text-red-300">
            ⚠️ Это событие было отменено. Новые бронирования недоступны.
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-(--border) pt-4">
          <div className="flex flex-wrap items-center gap-3">
            {isCancelled ? (
              <button
                type="button"
                disabled
                className="cursor-not-allowed rounded-lg bg-gray-200 px-6 py-2.5 text-sm font-semibold text-gray-500 dark:bg-gray-800 dark:text-gray-400"
              >
                Событие отменено
              </button>
            ) : isSoldOut ? (
              <button
                type="button"
                disabled
                className="cursor-not-allowed rounded-lg bg-gray-200 px-6 py-2.5 text-sm font-semibold text-gray-500 dark:bg-gray-800 dark:text-gray-400"
              >
                Все места распроданы
              </button>
            ) : (
              <Link
                to={`/bookings/create/${event.id}`}
                className="rounded-lg bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-indigo-700"
              >
                Забронировать места
              </Link>
            )}

            {isAdmin && (
              <Link
                to={`/events/${event.id}/edit`}
                className="rounded-lg border border-(--border) px-4 py-2.5 text-sm font-medium text-(--text-h) transition-colors hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                ✏️ Редактировать
              </Link>
            )}
          </div>

          {/* Административные действия: отмена и удаление */}
          {isAdmin && (
            <div className="flex items-center gap-2">
              {!isCancelled && (
                <Form
                  method="post"
                  onSubmit={(e) => {
                    if (
                      !window.confirm(
                        'Вы уверены, что хотите отменить это событие? Все существующие бронирования будут аннулированы.',
                      )
                    ) {
                      e.preventDefault();
                    }
                  }}
                >
                  <input type="hidden" name="intent" value="cancel" />
                  <button
                    type="submit"
                    disabled={isCancelling}
                    className="flex items-center gap-1.5 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2.5 text-sm font-medium text-amber-800 transition-colors hover:bg-amber-100 disabled:opacity-50 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-300 dark:hover:bg-amber-900/50"
                  >
                    {isCancelling ? 'Отмена события...' : '🚫 Отменить событие'}
                  </button>
                </Form>
              )}

              <Form
                method="post"
                onSubmit={(e) => {
                  if (
                    !window.confirm(
                      'Вы уверены, что хотите удалить это событие? Это действие необратимо.',
                    )
                  ) {
                    e.preventDefault();
                  }
                }}
              >
                <input type="hidden" name="intent" value="delete" />
                <button
                  type="submit"
                  disabled={isDeleting}
                  className="flex items-center gap-1.5 rounded-lg border border-red-200 bg-red-50 px-4 py-2.5 text-sm font-medium text-red-700 transition-colors hover:bg-red-100 disabled:opacity-50 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300 dark:hover:bg-red-900/50"
                >
                  {isDeleting ? 'Удаление...' : '🗑️ Удалить'}
                </button>
              </Form>
            </div>
          )}
        </div>
      </div>

      {/* Секция инспекции резерваций (только для администраторов) */}
      {isAdmin && (
        <div className="space-y-4 rounded-2xl border border-(--border) bg-(--bg) p-6 shadow-sm">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-bold text-(--text-h)">
              Резервации мест ({reservations.length})
            </h2>
            <span className="text-xs text-(--text)">Только для администраторов</span>
          </div>

          {reservations.length === 0 ? (
            <p className="py-4 text-center text-sm text-(--text)">
              На это событие пока нет зарезервированных мест.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-(--border) text-(--text)">
                    <th className="py-2.5 pr-2">Booking ID</th>
                    <th className="px-2 py-2.5">Места</th>
                    <th className="px-2 py-2.5">Цена за место</th>
                    <th className="px-2 py-2.5">Итого</th>
                    <th className="px-2 py-2.5">Статус</th>
                    <th className="py-2.5 pl-2">Создано</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-(--border)">
                  {reservations.map((r) => (
                    <tr
                      key={`${r.bookingId}-${r.priceVersion}`}
                      className="hover:bg-(--code-bg)/50"
                    >
                      <td className="py-2.5 pr-2 font-mono text-(--text-h)">
                        <Link
                          to={`/bookings/${r.bookingId}`}
                          className="text-indigo-600 hover:underline dark:text-indigo-400"
                        >
                          {r.bookingId.slice(0, 8)}...
                        </Link>
                      </td>
                      <td className="px-2 py-2.5 font-medium">{r.seats}</td>
                      <td className="px-2 py-2.5">{formatPrice(r.unitPrice, r.currency)}</td>
                      <td className="px-2 py-2.5 font-semibold">
                        {formatPrice(r.totalPrice, r.currency)}
                      </td>
                      <td className="px-2 py-2.5">
                        <span
                          className={`inline-block rounded px-2 py-0.5 text-[10px] font-semibold ${
                            r.status === 'Reserved'
                              ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-300'
                              : r.status === 'Pending'
                                ? 'bg-amber-100 text-amber-800 dark:bg-amber-950/50 dark:text-amber-300'
                                : 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300'
                          }`}
                        >
                          {r.status}
                        </span>
                      </td>
                      <td className="py-2.5 pl-2 text-(--text)">
                        {r.createdAt ? new Date(r.createdAt).toLocaleString('ru-RU') : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
