import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import {
  getUser,
  setUser,
  clearUser,
  isAuthenticated,
  isAdmin,
  subscribe,
  getToken,
  setToken,
  clearToken,
} from '../sessionStore';
import { useCurrentUser, useIsAuthenticated, useIsAdmin, useToken } from '../useAuthSession';
import type { User } from '../user';

const mockUser: User = {
  id: 'user-123',
  userName: 'testuser',
  role: 'User',
};

const mockAdmin: User = {
  id: 'admin-456',
  userName: 'adminuser',
  role: 'Admin',
};

describe('sessionStore.ts and useAuthSession.ts', () => {
  beforeEach(() => {
    localStorage.clear();
    clearUser();
    vi.restoreAllMocks();
  });

  it('returns null and false when no user is set', () => {
    expect(getUser()).toBeNull();
    expect(isAuthenticated()).toBe(false);
    expect(isAdmin()).toBe(false);
    expect(getToken()).toBeNull();
  });

  it('stores user, sets flags, and notifies listeners upon setUser', () => {
    const listener = vi.fn();
    const unsubscribe = subscribe(listener);

    setUser(mockUser);

    expect(getUser()).toEqual(mockUser);
    expect(isAuthenticated()).toBe(true);
    expect(isAdmin()).toBe(false);
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it('identifies Admin role correctly with isAdmin', () => {
    setUser(mockAdmin);

    expect(getUser()).toEqual(mockAdmin);
    expect(isAuthenticated()).toBe(true);
    expect(isAdmin()).toBe(true);
  });

  it('clears user, resets flags, and notifies listeners upon clearUser', () => {
    const listener = vi.fn();
    setUser(mockUser);

    const unsubscribe = subscribe(listener);
    clearUser();

    expect(getUser()).toBeNull();
    expect(isAuthenticated()).toBe(false);
    expect(isAdmin()).toBe(false);
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it('handles cross-tab storage events and unsubscribes cleanly', () => {
    const listener = vi.fn();
    const unsubscribe = subscribe(listener);

    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'auth_sync',
        newValue: Date.now().toString(),
      }),
    );

    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();

    window.dispatchEvent(
      new StorageEvent('storage', {
        key: 'auth_sync',
        newValue: Date.now().toString(),
      }),
    );

    expect(listener).toHaveBeenCalledTimes(1);
  });

  it('safely catches errors when localStorage is disabled or throws', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('Quota exceeded');
    });

    expect(() => setUser(mockUser)).not.toThrow();
    expect(() => clearUser()).not.toThrow();
  });

  it('supports backwards compatibility helpers getToken, setToken, clearToken', () => {
    expect(getToken()).toBeNull();

    setToken(mockUser);
    expect(getUser()).toEqual(mockUser);

    setToken('invalid-primitive-token');
    expect(getUser()).toEqual(mockUser);

    clearToken();
    expect(getUser()).toBeNull();

    setToken('token_for_legacy_user');
    expect(getUser()).toEqual({ id: 'legacy_admin_id', userName: 'Admin', role: 'Admin' });
    expect(getToken()).toBe('token_for_legacy_user');

    setToken(undefined);
  });

  it('reactively updates useCurrentUser, useIsAuthenticated, useIsAdmin, and useToken hooks', () => {
    const { result, unmount } = renderHook(() => ({
      user: useCurrentUser(),
      isAuth: useIsAuthenticated(),
      admin: useIsAdmin(),
      token: useToken(),
    }));

    expect(result.current.user).toBeNull();
    expect(result.current.isAuth).toBe(false);
    expect(result.current.admin).toBe(false);
    expect(result.current.token).toBeNull();

    act(() => {
      setUser(mockAdmin);
    });

    expect(result.current.user).toEqual(mockAdmin);
    expect(result.current.isAuth).toBe(true);
    expect(result.current.admin).toBe(true);

    act(() => {
      clearUser();
    });

    expect(result.current.user).toBeNull();
    expect(result.current.isAuth).toBe(false);
    expect(result.current.admin).toBe(false);

    unmount();
  });

  it('returns default fallback values during SSR snapshot', async () => {
    const { createElement } = await import('react');
    const { renderToString } = await import('react-dom/server');

    function SsrComponent() {
      const user = useCurrentUser();
      const isAuth = useIsAuthenticated();
      const admin = useIsAdmin();
      const token = useToken();

      return createElement(
        'div',
        { 'data-testid': 'ssr' },
        `${user === null}-${isAuth}-${admin}-${token === null}`,
      );
    }

    const html = renderToString(createElement(SsrComponent));
    expect(html).toContain('true-false-false-true');
  });
});
