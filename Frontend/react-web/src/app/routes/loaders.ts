import {
  requireAuthLoader as baseRequireAuthLoader,
  requireAdminLoader as baseRequireAdminLoader,
  anonymousOnlyLoader as baseAnonymousOnlyLoader,
  rootLoader as baseRootLoader,
} from '@/shared/lib/auth';

export const requireAuthLoader = baseRequireAuthLoader;
export const requireAdminLoader = baseRequireAdminLoader;
export const anonymousOnlyLoader = baseAnonymousOnlyLoader;
export const rootLoader = baseRootLoader;
