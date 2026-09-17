import { describe, it, expect } from 'vitest';
import { getSafeReturnUrl } from '../getSafeReturnUrl';

describe('getSafeReturnUrl (Open Redirect Prevention)', () => {
  it('returns "/" when target is null, undefined or empty string', () => {
    expect(getSafeReturnUrl(null)).toBe('/');
    expect(getSafeReturnUrl(undefined)).toBe('/');
    expect(getSafeReturnUrl('')).toBe('/');
    // @ts-expect-error test non-string types
    expect(getSafeReturnUrl(123)).toBe('/');
  });

  it('rejects external absolute URLs with protocols', () => {
    expect(getSafeReturnUrl('https://evil.com')).toBe('/');
    expect(getSafeReturnUrl('http://attacker.org')).toBe('/');
    expect(getSafeReturnUrl('ftp://example.com')).toBe('/');
    expect(getSafeReturnUrl('javascript:alert(1)')).toBe('/');
    expect(getSafeReturnUrl('data:text/html,<script>alert(1)</script>')).toBe('/');
  });

  it('rejects protocol-relative URLs (starts with //)', () => {
    expect(getSafeReturnUrl('//evil.com')).toBe('/');
    expect(getSafeReturnUrl('//evil.com/phishing')).toBe('/');
    expect(getSafeReturnUrl('///evil.com')).toBe('/');
  });

  it('rejects backslash bypass attempts (/\\evil.com)', () => {
    expect(getSafeReturnUrl('/\\evil.com')).toBe('/');
    expect(getSafeReturnUrl('/\\\\attacker.com')).toBe('/');
  });

  it('rejects URLs not starting with leading slash', () => {
    expect(getSafeReturnUrl('evil.com')).toBe('/');
    expect(getSafeReturnUrl('www.google.com')).toBe('/');
    expect(getSafeReturnUrl('events/123')).toBe('/');
  });

  it('allows safe relative application paths', () => {
    expect(getSafeReturnUrl('/events')).toBe('/events');
    expect(getSafeReturnUrl('/events/123')).toBe('/events/123');
    expect(getSafeReturnUrl('/events/123?tab=info&highlight=true')).toBe(
      '/events/123?tab=info&highlight=true',
    );
    expect(getSafeReturnUrl('/bookings/create/456')).toBe('/bookings/create/456');
    expect(getSafeReturnUrl('/profile')).toBe('/profile');
  });
});
