import { describe, it, expect } from 'vitest';
import { normalizeFieldKey, isProblemDetails, toFormError } from '../errors';

describe('errors.ts', () => {
  describe('normalizeFieldKey', () => {
    it('normalizes single PascalCase field to camelCase', () => {
      expect(normalizeFieldKey('Title')).toBe('title');
      expect(normalizeFieldKey('TotalSeats')).toBe('totalSeats');
    });

    it('normalizes nested dot-notation fields', () => {
      expect(normalizeFieldKey('Event.TotalSeats')).toBe('event.totalSeats');
      expect(normalizeFieldKey('User.Profile.Email')).toBe('user.profile.email');
    });

    it('preserves array index brackets while lowercasing segment name', () => {
      expect(normalizeFieldKey('Event.TotalSeats[0]')).toBe('event.totalSeats[0]');
      expect(normalizeFieldKey('Items[12]')).toBe('items[12]');
    });

    it('returns empty string unchanged', () => {
      expect(normalizeFieldKey('')).toBe('');
    });
  });

  describe('isProblemDetails', () => {
    it('returns true when object contains errors dictionary', () => {
      expect(isProblemDetails({ errors: { title: ['Required'] } })).toBe(true);
    });

    it('returns true when object contains extensions with errors', () => {
      expect(isProblemDetails({ extensions: { errors: { email: ['Invalid'] } } })).toBe(true);
    });

    it('returns true when HTTP status in body is >= 400', () => {
      expect(isProblemDetails({ status: 400, title: 'Bad Request' })).toBe(true);
      expect(isProblemDetails({ status: 500 })).toBe(true);
    });

    it('returns true when explicit isSuccess is false', () => {
      expect(isProblemDetails({ isSuccess: false, message: 'Failed' })).toBe(true);
    });

    it('returns false for non-problem objects, primitives and null', () => {
      expect(isProblemDetails(null)).toBe(false);
      expect(isProblemDetails(undefined)).toBe(false);
      expect(isProblemDetails('error string')).toBe(false);
      expect(isProblemDetails(123)).toBe(false);
      expect(isProblemDetails({ status: 200, isSuccess: true })).toBe(false);
      expect(isProblemDetails({ data: 'ok' })).toBe(false);
    });
  });

  describe('toFormError', () => {
    it('returns null for falsy error values', () => {
      expect(toFormError(null)).toBeNull();
      expect(toFormError(undefined)).toBeNull();
    });

    it('handles string errors', () => {
      const res = toFormError('Что-то пошло не так');
      expect(res).toEqual({
        message: 'Что-то пошло не так',
        fieldErrors: undefined,
      });
    });

    it('translates network error string into user-friendly message', () => {
      const res = toFormError('Failed to fetch');
      expect(res?.message).toContain('Сервер временно недоступен');
    });

    it('handles standard JS Error instances and network Error', () => {
      const err = new Error('Custom runtime error');
      expect(toFormError(err)).toEqual({
        message: 'Custom runtime error',
        fieldErrors: undefined,
      });

      const networkErr = new TypeError('NetworkError when attempting to fetch resource.');
      expect(toFormError(networkErr)?.message).toContain('Сервер временно недоступен');
    });

    it('parses RFC 7807 ProblemDetails with errors and normalizes keys', () => {
      const problem = {
        title: 'Validation failed',
        detail: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          'Event.Title': ['Название обязательно'],
          'TotalSeats[0]': ['Должно быть больше нуля'],
        },
      };

      const res = toFormError(problem);
      expect(res?.message).toBe('One or more validation errors occurred.');
      expect(res?.fieldErrors).toEqual({
        'event.title': ['Название обязательно'],
        'totalSeats[0]': ['Должно быть больше нуля'],
      });
    });

    it('supports fieldNameMap translation', () => {
      const problem = {
        errors: {
          UserName: ['Имя занято'],
        },
      };

      const res = toFormError(problem, {
        fieldNameMap: { userName: 'username' },
      });

      expect(res?.fieldErrors).toEqual({
        username: ['Имя занято'],
      });
    });

    it('attaches error to localFieldName when requested', () => {
      const res = toFormError<{ username: string }>('Логин не найден', {
        localFieldName: 'username',
      });

      expect(res).toEqual({
        message: 'Логин не найден',
        fieldErrors: {
          username: ['Логин не найден'],
        },
      });
    });

    it('falls back to defaultMessage for unknown primitives', () => {
      const res = toFormError(404, { defaultMessage: 'Ресурс не найден' });
      expect(res).toEqual({
        message: 'Ресурс не найден',
        fieldErrors: undefined,
      });
    });
  });
});
