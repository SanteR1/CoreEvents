import { redirect, type LoaderFunctionArgs } from 'react-router';
import { getUser, setUser } from './sessionStore';
import { getCurrentUser } from '@/features/auth/api/authApi';
import { getSafeReturnUrl } from './getSafeReturnUrl';
import type { User } from './user';

let sessionPromise: Promise<User | null> | null = null;

export function resetSessionPromise(): void {
  sessionPromise = null;
}

/**
 * Инициализирует сессию пользователя через GET /v1/users/me.
 * Предотвращает дублирующие параллельные сетевые запросы с помощью единого промиса.
 */
export async function ensureSession(): Promise<User | null> {
  const existing = getUser();
  if (existing) {
    return existing;
  }

  sessionPromise ??= getCurrentUser()
    .then((user) => {
      setUser(user);
      return user;
    })
    .finally(() => {
      sessionPromise = null;
    });

  return sessionPromise;
}

/**
 * Корневой загрузчик для роутера.
 */
export async function rootLoader() {
  const user = await ensureSession();
  return { user };
}

/**
 * Защищает приватные маршруты.
 * Если пользователя нет — перенаправляет на /login с returnUrl.
 */
export async function requireAuthLoader({ request }: { request: Request } | LoaderFunctionArgs) {
  const user = await ensureSession();

  if (!user) {
    const url = new URL(request.url);
    const returnUrl = encodeURIComponent(url.pathname + url.search);
    return redirect(`/login?returnUrl=${returnUrl}`);
  }

  return null;
}

/**
 * Защищает административные маршруты.
 * Требует авторизованную сессию и роль "Admin".
 * Для неавторизованных гостей — redirect на /login.
 * Для авторизованных не-админов — 403 Forbidden.
 */
export async function requireAdminLoader({ request }: { request: Request } | LoaderFunctionArgs) {
  const user = await ensureSession();

  if (!user) {
    const url = new URL(request.url);
    const returnUrl = encodeURIComponent(url.pathname + url.search);
    return redirect(`/login?returnUrl=${returnUrl}`);
  }

  if (user.role !== 'Admin') {
    throw new Response('Доступ запрещен: требуются права администратора', {
      status: 403,
      statusText: 'Forbidden',
    });
  }

  return null;
}

/**
 * Для публичных страниц авторизации (Login / Register).
 * Если пользователь уже вошел, перенаправляет на главную или returnUrl.
 */
export async function anonymousOnlyLoader({ request }: LoaderFunctionArgs | { request: Request }) {
  const user = await ensureSession();

  if (user) {
    const url = new URL(request.url);
    const returnUrl = url.searchParams.get('returnUrl');
    return redirect(getSafeReturnUrl(returnUrl));
  }

  return null;
}
