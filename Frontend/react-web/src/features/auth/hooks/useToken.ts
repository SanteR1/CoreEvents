import {
  useToken as baseUseToken,
  useIsAuthenticated as baseUseIsAuthenticated,
  useIsAdmin as baseUseIsAdmin,
  useCurrentUser as baseUseCurrentUser,
} from '@/shared/lib/auth';

export const useToken = baseUseToken;
export const useIsAuthenticated = baseUseIsAuthenticated;
export const useIsAdmin = baseUseIsAdmin;
export const useCurrentUser = baseUseCurrentUser;
