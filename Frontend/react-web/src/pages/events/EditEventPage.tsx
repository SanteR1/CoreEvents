import {
  useLoaderData,
  useActionData,
  useNavigation,
  type ActionFunctionArgs,
  type LoaderFunctionArgs,
  redirect,
  Link,
} from 'react-router';
import { requireAuthLoader } from '@/app/routes/loaders';
import {
  getEventById,
  updateEventById,
  deleteEventById,
  type EventUpdate,
} from '@/features/events/api/eventsApi';
import { EventEditForm } from '@/features/events/components/EventEditForm';
import { toFormError, type FieldErrors } from '@/shared/api/errors';

export async function loader(args: LoaderFunctionArgs) {
  const authRedirect = requireAuthLoader(args);
  if (authRedirect) {
    return authRedirect;
  }

  const eventId = args.params.eventId ?? args.params.id;
  if (!eventId) {
    throw new Response('Идентификатор события не указан', { status: 404 });
  }

  const res = await getEventById(eventId);
  if (!res.success || !res.event) {
    throw new Response('Событие не найдено', { status: 404 });
  }

  return { event: res.event };
}

export async function action({ request, params }: ActionFunctionArgs) {
  const eventId = params.eventId ?? params.id;
  if (!eventId) {
    return { error: toFormError('Идентификатор события не найден') };
  }

  const formData = await request.formData();
  const intent = formData.get('intent');

  if (intent === 'delete') {
    const res = await deleteEventById(eventId);
    if (!res.success) {
      return { error: res.error };
    }
    return redirect('/');
  }

  const rawTitle = formData.get('title');
  const title = typeof rawTitle === 'string' ? rawTitle.trim() : '';

  const rawStartAt = formData.get('startAt');
  const startAtRaw = typeof rawStartAt === 'string' ? rawStartAt.trim() : '';

  const rawEndAt = formData.get('endAt');
  const endAtRaw = typeof rawEndAt === 'string' ? rawEndAt.trim() : '';

  const rawDesc = formData.get('description');
  const descriptionRaw = typeof rawDesc === 'string' ? rawDesc.trim() : '';
  const description = descriptionRaw ? descriptionRaw : null;

  const fieldErrors: FieldErrors = {};
  if (!title) {
    fieldErrors.title = ['Название события обязательно для заполнения'];
  }
  if (!startAtRaw) {
    fieldErrors.startAt = ['Укажите дату и время начала'];
  }
  if (!endAtRaw) {
    fieldErrors.endAt = ['Укажите дату и время окончания'];
  }

  const startDate = new Date(startAtRaw);
  const endDate = new Date(endAtRaw);

  if (startAtRaw && endAtRaw) {
    if (isNaN(startDate.getTime())) {
      fieldErrors.startAt = ['Некорректный формат даты начала'];
    }
    if (isNaN(endDate.getTime())) {
      fieldErrors.endAt = ['Некорректный формат даты окончания'];
    }
    if (endDate <= startDate) {
      fieldErrors.endAt = ['Дата окончания должна быть позже даты начала'];
    }
  }

  if (Object.keys(fieldErrors).length > 0) {
    return {
      error: {
        message: 'Пожалуйста, исправьте ошибки в форме',
        fieldErrors,
      },
    };
  }

  try {
    const payload: EventUpdate = {
      title,
      startAt: startDate,
      endAt: endDate,
      description,
    };

    const res = await updateEventById(eventId, payload);

    if (!res.success) {
      return {
        error: res.error,
      };
    }

    return redirect(`/events/${eventId}`);
  } catch (err) {
    if (err instanceof Response) {
      throw err;
    }
    return {
      error: toFormError(err, {
        defaultMessage: 'Произошла непредвиденная ошибка при обновлении события',
      }),
    };
  }
}

export const EditEventPage = () => {
  const { event } = useLoaderData<typeof loader>();
  const actionData = useActionData<typeof action>();
  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  return (
    <div className="mx-auto max-w-3xl space-y-6 py-6 text-left">
      <Link
        to={`/events/${event.id}`}
        className="inline-flex items-center gap-1 text-sm font-medium text-[var(--text)] transition-colors hover:text-[var(--text-h)]"
      >
        ← Назад к событию
      </Link>

      <div>
        <h1 className="text-2xl font-extrabold text-[var(--text-h)] sm:text-3xl">
          Редактирование события
        </h1>
        <p className="mt-1 text-sm text-[var(--text)]">
          Внесите изменения в информацию о событии и сохраните их.
        </p>
      </div>

      <EventEditForm event={event} error={actionData?.error} isSubmitting={isSubmitting} />
    </div>
  );
};
