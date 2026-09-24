import createClient from 'openapi-fetch';
import type { paths } from '@/shared/api/generated/bookings'; // Путь к сгенерированным типам
import { getToken } from '@/shared/lib/auth/sessionStore';

const BOOKINGS_API_URL: string =
  (import.meta.env.VITE_API_URL as string | undefined) ??
  (import.meta.env.VITE_API_GATEWAY_URL as string | undefined) ??
  (import.meta.env.VITE_BOOKINGS_API_URL as string | undefined) ??
  'http://localhost:5000/v1';

export const bookingsClient = createClient<paths>({
  baseUrl: BOOKINGS_API_URL,
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
