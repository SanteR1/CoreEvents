import createClient from 'openapi-fetch';
import type { paths } from '@/shared/api/generated/users';
import { getToken } from '@/shared/lib/auth/sessionStore';
import { authRefreshMiddleware } from './authRefreshMiddleware';

const USERS_API_URL: string =
  (import.meta.env.VITE_API_URL as string | undefined) ??
  (import.meta.env.VITE_API_GATEWAY_URL as string | undefined) ??
  (import.meta.env.VITE_USERS_API_URL as string | undefined) ??
  'http://localhost:5000/v1';

export const usersClient = createClient<paths>({
  baseUrl: USERS_API_URL,
  credentials: 'include',
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
usersClient.use(authRefreshMiddleware);
