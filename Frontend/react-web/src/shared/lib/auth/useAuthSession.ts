import { useSyncExternalStore } from 'react';
import { getUser, isAuthenticated, isAdmin, getToken, subscribe } from './sessionStore';
import type { User } from './user';

/**
 * Реактивно возвращает текущего авторизованного пользователя.
 */
export function useCurrentUser(): User | null {
  return useSyncExternalStore(subscribe, getUser, () => null);
}

/**
 * Реактивно возвращает статус аутентификации пользователя (boolean).
 */
export function useIsAuthenticated(): boolean {
  return useSyncExternalStore(subscribe, isAuthenticated, () => false);
}

/**
 * Реактивно возвращает флаг наличия прав администратора (boolean).
 */
export function useIsAdmin(): boolean {
  return useSyncExternalStore(subscribe, isAdmin, () => false);
}

/**
 * Для обратной совместимости.
 */
export function useToken(): string | null {
  return useSyncExternalStore(subscribe, getToken, () => null);
}
