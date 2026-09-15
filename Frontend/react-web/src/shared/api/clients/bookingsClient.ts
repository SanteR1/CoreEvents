import createClient from 'openapi-fetch';
import type { paths } from '@/shared/api/generated/bookings'; // Путь к сгенерированным типам
import { getToken } from '@/shared/lib/auth/sessionStore';

const BOOKINGS_API_URL = import.meta.env.VITE_BOOKINGS_API_URL as string;

export const bookingsClient = createClient<paths>({
  //baseUrl: 'http://host.docker.internal:5005', // Или ваш базовый URL для продакшена/разработки
  baseUrl: `${BOOKINGS_API_URL}`, // Или ваш базовый URL для продакшена/разработки
});

// Опционально: можно добавить интерцепторы (middleware) для авторизации
bookingsClient.use({
  onRequest({ request }) {
    const token = getToken();
    if (token) {
      request.headers.set('Authorization', `Bearer ${token}`);
    }
    return request;
  },
});
