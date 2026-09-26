import { clearUser } from '@/shared/lib/auth/sessionStore';

let refreshPromise: Promise<boolean> | null = null;

export function getRefreshPromise(): Promise<boolean> | null {
  return refreshPromise;
}

export function resetRefreshPromise(): void {
  refreshPromise = null;
}

export async function refreshAuthSession(): Promise<boolean> {
  if (refreshPromise) {
    return refreshPromise;
  }

  const gatewayUrl =
    (import.meta.env.VITE_API_URL as string | undefined) ??
    (import.meta.env.VITE_API_GATEWAY_URL as string | undefined) ??
    (import.meta.env.VITE_USERS_API_URL as string | undefined) ??
    'http://localhost:5000';

  refreshPromise = (async () => {
    try {
      const response = await fetch(`${gatewayUrl}/v1/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
      });
      return response.ok;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

export const authRefreshMiddleware = {
  async onResponse({
    request,
    response,
  }: {
    request: Request;
    response: Response;
  }): Promise<Response | undefined> {
    const url = request.url.toLowerCase();
    const isAuthEndpoint =
      url.includes('/auth/refresh') ||
      url.includes('/auth/login') ||
      url.includes('/auth/register');

    if (response.status === 401 && !isAuthEndpoint) {
      const refreshed = await refreshAuthSession();
      if (refreshed) {
        return fetch(request.clone());
      }
      clearUser();
    }

    return response;
  },
};
