import { useSyncExternalStore } from 'react';
import { getToken, subscribe } from '@/shared/lib/auth/sessionStore';

// Это лучше чем useAuth, потому что useAuth возвращает объект, а useToken возвращает только токен.
export function useToken(): string | null {
  return useSyncExternalStore(
    subscribe,
    getToken,
    () => null, // getSnapshot для SSR (если потребуется)
  );
}

export function useIsAuthenticated(): boolean {
  const token = useToken();
  return Boolean(token);
}
