import { useSyncExternalStore } from 'react';
import { getToken, subscribe } from './sessionStore';

/**
 * Реактивно возвращает текущий токен сессии.
 * Синхронизируется при setToken/clearToken и изменениях localStorage между вкладками.
 */
export function useToken(): string | null {
  return useSyncExternalStore(subscribe, getToken, () => null);
}

/**
 * Реактивно возвращает статус аутентификации пользователя (boolean).
 */
export function useIsAuthenticated(): boolean {
  const token = useToken();
  return Boolean(token);
}
