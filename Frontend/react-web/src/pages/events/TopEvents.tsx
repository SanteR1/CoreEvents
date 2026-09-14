// src/pages/events/TopEvents.tsx
import { useLoaderData, useNavigate } from 'react-router';
import { getTopEvents } from '@/features/events/api/eventsApi';
import { TopEventsForm } from '@/features/events/components/TopEventsForm';

// 1. Loader функция (запускается автоматически при открытии страницы)
export async function loader() {
  try {
    const res = await getTopEvents();
    return { res };
  } catch (err) {
    return {
      error: err instanceof Error ? err.message : 'Не удалось найти события',
      //data: null, // Возвращаем null, чтобы типизация не ругалась
      //status: 500,
    };
  }
}

// 2. Компонент страницы
export const TopEventsPage = () => {
  const { res } = useLoaderData<typeof loader>();
  // Достаем navigate для программного перехода по клику
  const navigate = useNavigate();

  // Пишем функцию, которая описывает реальное действие
  const handleBooking = (eventId: string) => {
    console.log('Пользователь хочет купить билет на:', eventId);

    // Используем navigate вместо redirect внутри компонента
    // Вариант А: Перекинуть на страницу чекаута (бронирования)
    //navigate(`/bookings/new?eventId=${eventId}`);
    void navigate(`/bookings/create/${eventId}`);
    // navigate(`/bookings/create?eventId=${eventId}&seats=1`);

    // Вариант Б: Сразу дернуть метод твоего сервиса Bookings
    // await createBooking({ eventId, userId: currentUser.id });
  };

  if (res?.success) {
    return (
      <div className="mx-auto max-w-3xl space-y-8 p-6">
        <div>
          <h1 className="text-2xl font-bold text-(--text-h)">Топ событий</h1>
          <p className="text-sm text-(--text)">Список популярных событий</p>
        </div>
        {/* Отрисовка созданного события, если loader завершился успехом */}
        {<TopEventsForm events={res.event} onBookClick={handleBooking} />}
      </div>
    );
  }
};
