import { useToken, useIsAuthenticated } from '@/shared/lib/auth';

export function useAuth() {
  const token = useToken();
  const isAuthenticated = useIsAuthenticated();
  return { token, isAuthenticated };
}
