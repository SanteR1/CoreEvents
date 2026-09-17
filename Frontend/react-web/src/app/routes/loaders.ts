import {
  requireAuthLoader as baseRequireAuthLoader,
  anonymousOnlyLoader as baseAnonymousOnlyLoader,
} from '@/shared/lib/auth';

export const requireAuthLoader = baseRequireAuthLoader;
export const anonymousOnlyLoader = baseAnonymousOnlyLoader;
