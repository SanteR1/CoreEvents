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
import { getEventById, deleteEventById } from '@/features/events/api/eventsApi';
import { requireAdminLoader } from '@/shared/lib/auth';
import { toFormError } from '@/shared/api/errors';
import { useAuth } from '@/features/auth/hooks/useAuth';

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

  return { event: res.event };
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

  return null;
}

export const GetEventByIdPage = () => {
  const { event } = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const { isAdmin } = useAuth();
  const isDeleting =
    navigation.state === 'submitting' && navigation.formData?.get('intent') === 'delete';

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

      {/* Ошибка при удалении события */}
      {actionData?.error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/20 dark:text-red-400">
          {actionData.error.message}
        </div>
      )}

      <div className="space-y-6 rounded-2xl border border-(--border) bg-(--bg) p-8 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <span className="rounded-md bg-(--code-bg) px-2.5 py-1 font-mono text-xs text-(--text)">
            ID: {event.id}
          </span>

          {isSoldOut ? (
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
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-(--border) pt-4">
          <div className="flex flex-wrap items-center gap-3">
            {isSoldOut ? (
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

          {/* Кнопка удаления события */}
          {isAdmin && (
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
          )}
        </div>
      </div>
    </div>
  );
};
