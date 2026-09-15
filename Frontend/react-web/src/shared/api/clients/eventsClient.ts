import createClient from 'openapi-fetch';
import type { paths } from '@/shared/api/generated/events'; // Путь к сгенерированным типам
import { getToken } from '@/shared/lib/auth/sessionStore';

const EVENTS_API_URL = import.meta.env.VITE_EVENTS_API_URL as string;

export const eventsClient = createClient<paths>({
  //baseUrl: 'http://host.docker.internal:5004', // Или ваш базовый URL для продакшена/разработки
  baseUrl: `${EVENTS_API_URL}`, // Или ваш базовый URL для продакшена/разработки
});

// Опционально: можно добавить интерцепторы (middleware) для авторизации
eventsClient.use({
  onRequest({ request }) {
    const token = getToken();
    if (token) {
      request.headers.set('Authorization', `Bearer ${token}`);
    }
    return request;
  },
});
