import createClient from 'openapi-fetch';
import type { paths } from '@/shared/api/generated/users'; // Путь к сгенерированным типам
import { getToken } from '@/shared/lib/auth/sessionStore';

const USERS_API_URL: string =
  (import.meta.env.VITE_USERS_API_URL as string | undefined) ?? 'http://localhost:5003';

export const usersClient = createClient<paths>({
  baseUrl: USERS_API_URL,
});

// Опционально: можно добавить интерцепторы (middleware) для авторизации
usersClient.use({
  onRequest({ request }) {
    const token = getToken();
    if (token) {
      request.headers.set('Authorization', `Bearer ${token}`);
    }
    return request;
  },
});
