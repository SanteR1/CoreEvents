import { useSyncExternalStore } from 'react';
import { getToken, subscribe } from '@/shared/lib/auth/sessionStore';

export function useAuth() {
  const token = useSyncExternalStore(subscribe, getToken, () => null);
  return { token, isAuthenticated: token !== null };
}
