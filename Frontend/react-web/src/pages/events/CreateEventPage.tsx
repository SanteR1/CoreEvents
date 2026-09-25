// src/pages/events/CreateEventPage.tsx
import {
  useActionData,
  useNavigation,
  type ActionFunctionArgs,
  type LoaderFunctionArgs,
} from 'react-router';
import { createEvent, type EventCreate } from '@/features/events/api/eventsApi';
import { EventRequestForm } from '@/features/events/components/EventRequestForm';
import { EventResponseForm } from '@/features/events/components/EventResponseForm';
import { toFormError, type FieldErrors } from '@/shared/api/errors';
import { requireAdminLoader } from '@/shared/lib/auth';

export async function loader(args: LoaderFunctionArgs) {
  const authRedirect = await requireAdminLoader(args);
  if (authRedirect) {
    return authRedirect;
  }
  return null;
}

// 1. Action функция роутера
export async function action({ request }: ActionFunctionArgs) {
  const authRedirect = await requireAdminLoader({ request });
  if (authRedirect) {
    return authRedirect;
  }

  const formData = await request.formData();

  const rawTitle = formData.get('title');
  const title = typeof rawTitle === 'string' ? rawTitle.trim() : '';

  const rawStartAt = formData.get('startAt');
  const startAtRaw = typeof rawStartAt === 'string' ? rawStartAt.trim() : '';

  const rawEndAt = formData.get('endAt');
  const endAtRaw = typeof rawEndAt === 'string' ? rawEndAt.trim() : '';

  const rawSeats = formData.get('totalSeats');
  const parsedSeats = typeof rawSeats === 'string' ? parseInt(rawSeats, 10) : 0;
  const totalSeats = Number.isNaN(parsedSeats) ? 0 : parsedSeats;

  const rawDesc = formData.get('description');
  const description = typeof rawDesc === 'string' ? rawDesc.trim() : '';

  const fieldErrors: FieldErrors = {};
  if (!title) {
    fieldErrors.title = ['Название события обязательно для заполнения'];
  }
  if (!startAtRaw) {
    fieldErrors.startAt = ['Укажите дату начала'];
  }
  if (!endAtRaw) {
    fieldErrors.endAt = ['Укажите дату окончания'];
  }
  if (totalSeats <= 0) {
    fieldErrors.totalSeats = ['Количество мест должно быть больше 0'];
  }

  // Проверка логики дат
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

  // Если есть хотя бы одна локальная ошибка — сразу возвращаем в форму
  if (Object.keys(fieldErrors).length > 0) {
    return {
      error: {
        message: 'Пожалуйста, исправьте ошибки в форме',
        fieldErrors,
      },
    };
  }

  // =========================================================================
  // 2. ОТПРАВКА НА WEB API И ПРИЁМ ОШИБОК С БЭКЕНДА
  // =========================================================================
  try {
    const payload: EventCreate = {
      title,
      startAt: startDate,
      endAt: endDate,
      totalSeats,
      description,
    };

    const res = await createEvent(payload);

    // Если бэкенд вернул 400 Bad Request (ValidationProblemDetails), 401, 403 или сеть упала
    if (!res.success) {
      return {
        error: res.error, // res.error уже обработан в eventsApi через toFormError
      };
    }

    // Успех
    return {
      event: res.event,
    };
  } catch (err) {
    if (err instanceof Response) {
      throw err;
    }
    return {
      error: toFormError(err, {
        defaultMessage: 'Произошла непредвиденная ошибка при сохранении события',
      }),
    };
  }
}

// 2. Компонент страницы
export const CreateEventPage = () => {
  const actionData = useActionData<typeof action>();

  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  // Если есть event, значит создание прошло успешно
  if (actionData?.event) {
    return (
      <div className="mx-auto max-w-3xl space-y-8 p-6">
        <div>
          <h1 className="text-2xl font-bold">Событие создано!</h1>
          <p className="text-sm text-gray-500">Событие успешно опубликовано</p>
        </div>
        <EventResponseForm event={actionData.event} />
      </div>
    );
  }

  // В противном случае рендерим форму (первый заход или попытка с ошибкой)
  return (
    <div className="mx-auto max-w-3xl space-y-8 p-6">
      <div>
        <h1 className="text-2xl font-bold">Создание события</h1>
        <p className="text-sm text-gray-500">Заполните данные для публикации нового события</p>
      </div>

      {/* Передаем готовую ошибку напрямую. actionData?.error уже является FormActionError */}
      <EventRequestForm error={actionData?.error} isSubmitting={isSubmitting} />
    </div>
  );
};
