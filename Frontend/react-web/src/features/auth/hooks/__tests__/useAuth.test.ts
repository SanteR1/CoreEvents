import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useAuth } from '../useAuth';
import { useToken, useIsAuthenticated } from '../useToken';
import { setToken, clearToken } from '@/shared/lib/auth';

function createMockJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({ sub: 'user_123', exp: Math.floor(Date.now() / 1000) + 3600 }),
  );
  return `${header}.${payload}.signature`;
}

describe('useAuth and useToken Hooks', () => {
  beforeEach(() => {
    localStorage.clear();
    clearToken();
  });

  afterEach(() => {
    localStorage.clear();
    clearToken();
  });

  it('returns unauthenticated state when no token is present', () => {
    const { result, unmount } = renderHook(() => useAuth());

    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
    unmount();
  });

  it('reactively updates state when setToken and clearToken are called', () => {
    const { result, unmount } = renderHook(() => useAuth());

    const jwt = createMockJwt();
    act(() => {
      setToken(jwt);
    });

    expect(result.current.token).toBe(jwt);
    expect(result.current.isAuthenticated).toBe(true);

    act(() => {
      clearToken();
    });

    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
    unmount();
  });

  it('verifies useToken re-export from useToken.ts matches store state', () => {
    const { result, unmount } = renderHook(() => useToken());
    expect(result.current).toBeNull();

    const jwt = createMockJwt();
    act(() => {
      setToken(jwt);
    });

    expect(result.current).toBe(jwt);
    unmount();
  });

  it('verifies useIsAuthenticated re-export from useToken.ts matches store state', () => {
    const { result, unmount } = renderHook(() => useIsAuthenticated());
    expect(result.current).toBe(false);

    const jwt = createMockJwt();
    act(() => {
      setToken(jwt);
    });

    expect(result.current).toBe(true);
    unmount();
  });
});
