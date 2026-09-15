import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { getToken, setToken, clearToken, subscribe } from '../sessionStore';
import { useToken, useIsAuthenticated } from '../useAuthSession';

function createMockJwt(expSecondsFromNow?: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payloadData =
    expSecondsFromNow !== undefined
      ? { exp: Math.floor(Date.now() / 1000) + expSecondsFromNow }
      : { sub: '123' };
  const payload = btoa(JSON.stringify(payloadData));
  const signature = 'fake_signature';
  return `${header}.${payload}.${signature}`;
}

describe('sessionStore.ts', () => {
  beforeEach(() => {
    localStorage.clear();
    clearToken();
  });

  it('returns null when no token is stored in localStorage', () => {
    expect(getToken()).toBeNull();
  });

  it('returns null when stored token is literal "undefined" or "null"', () => {
    localStorage.setItem('auth_token', 'undefined');
    expect(getToken()).toBeNull();

    localStorage.setItem('auth_token', 'null');
    expect(getToken()).toBeNull();
  });

  it('returns valid token when JWT is active and not expired', () => {
    const validToken = createMockJwt(3600); // 1 час вперед
    setToken(validToken);

    expect(getToken()).toBe(validToken);
  });

  it('returns token conditionally valid when exp field is absent', () => {
    const tokenWithoutExp = createMockJwt(undefined);
    setToken(tokenWithoutExp);

    expect(getToken()).toBe(tokenWithoutExp);
  });

  it('detects expired JWT, immediately returns null, and defers removal to microtask', async () => {
    const expiredToken = createMockJwt(-60); // протух 60 секунд назад
    localStorage.setItem('auth_token', expiredToken);

    // 1. Непосредственный вызов должен вернуть null (чистый getter)
    const token = getToken();
    expect(token).toBeNull();

    // 2. В синхронном цикле localStorage ещё не должен мутироваться (side-effect отложен)
    expect(localStorage.getItem('auth_token')).toBe(expiredToken);

    // 3. Явный флаш очереди микротасок
    await Promise.resolve();

    // 4. После микротаски токен удалён из хранилища
    expect(localStorage.getItem('auth_token')).toBeNull();
  });

  it('handles malformed JWT strings as expired/invalid', async () => {
    localStorage.setItem('auth_token', 'not.a.valid.jwt.payload');

    expect(getToken()).toBeNull();

    await Promise.resolve();
    expect(localStorage.getItem('auth_token')).toBeNull();
  });

  it('stores token and notifies listeners upon setToken', () => {
    const listener = vi.fn();
    const unsubscribe = subscribe(listener);

    const token = createMockJwt(3600);
    setToken(token);

    expect(localStorage.getItem('auth_token')).toBe(token);
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it('removes token and notifies listeners upon clearToken', () => {
    const token = createMockJwt(3600);
    setToken(token);

    const listener = vi.fn();
    const unsubscribe = subscribe(listener);

    clearToken();

    expect(localStorage.getItem('auth_token')).toBeNull();
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it('responds to StorageEvent across tabs and unbinds cleanly', () => {
    const listener = vi.fn();
    const unsubscribe = subscribe(listener);

    // Имитируем событие storage из другой вкладки браузера
    window.dispatchEvent(new StorageEvent('storage'));
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();

    window.dispatchEvent(new StorageEvent('storage'));
    expect(listener).toHaveBeenCalledTimes(1); // Не должен вызываться повторно
  });

  it('useToken and useIsAuthenticated hooks reactively update with session store', () => {
    const { result } = renderHook(() => ({
      token: useToken(),
      isAuthenticated: useIsAuthenticated(),
    }));

    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);

    const token = createMockJwt(3600);
    act(() => {
      setToken(token);
    });

    expect(result.current.token).toBe(token);
    expect(result.current.isAuthenticated).toBe(true);

    act(() => {
      clearToken();
    });

    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
  });
});
