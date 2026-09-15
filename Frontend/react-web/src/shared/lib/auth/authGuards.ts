import { redirect, type LoaderFunctionArgs } from 'react-router';
import { getToken } from './sessionStore';

/**
 * Защищает приватные маршруты.
 * Если токена нет — прерывает навигацию ДО рендера компонента
 * и делает redirect на /login, передавая текущий URL в query-параметрах.
 */
export function requireAuthLoader({ request }: { request: Request }) {
  const token = getToken();

  if (!token) {
    const url = new URL(request.url);
    const returnUrl = encodeURIComponent(url.pathname + url.search);
    return redirect(`/login?returnUrl=${returnUrl}`);
  }

  return null;
}

/**
 * Для публичных страниц авторизации (Login / Register).
 * Если токен уже есть, незачем показывать форму входа — отправляем на главную.
 */
export function anonymousOnlyLoader({ request }: LoaderFunctionArgs) {
  const token = getToken();

  if (token) {
    const url = new URL(request.url);
    const returnUrl = url.searchParams.get('returnUrl');
    return redirect(returnUrl ?? '/');
  }

  return null;
}
