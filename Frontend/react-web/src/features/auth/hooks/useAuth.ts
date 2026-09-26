import { useCurrentUser, useIsAuthenticated, useIsAdmin, useToken } from '@/shared/lib/auth';
import type { User } from '@/shared/lib/auth';

export interface UseAuthReturn {
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  token: string | null;
}

export function useAuth(): UseAuthReturn {
  const user = useCurrentUser();
  const isAuthenticated = useIsAuthenticated();
  const isAdmin = useIsAdmin();
  const token = useToken();

  return { user, isAuthenticated, isAdmin, token };
}
