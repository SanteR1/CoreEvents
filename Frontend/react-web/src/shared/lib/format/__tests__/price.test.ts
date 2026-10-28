import { describe, it, expect } from 'vitest';
import { formatPrice } from '../price';

describe('formatPrice', () => {
  it('returns "Бесплатно" when price is 0', () => {
    expect(formatPrice(0)).toBe('Бесплатно');
  });

  it('returns "Бесплатно" when price is negative', () => {
    expect(formatPrice(-100)).toBe('Бесплатно');
  });

  it('formats KZT currency with ₸ symbol and Russian thousand grouping', () => {
    // Note: ru-RU toLocaleString might use non-breaking space (\u00A0) or thin space
    const formatted = formatPrice(1500, 'KZT');
    expect(formatted.replace(/\s/g, ' ')).toBe('1 500 ₸');

    const formattedLarge = formatPrice(250000, 'KZT');
    expect(formattedLarge.replace(/\s/g, ' ')).toBe('250 000 ₸');
  });

  it('defaults to KZT when currency parameter is omitted', () => {
    const formatted = formatPrice(500);
    expect(formatted.replace(/\s/g, ' ')).toBe('500 ₸');
  });

  it('handles lowercase kzt currency string', () => {
    const formatted = formatPrice(1000, 'kzt');
    expect(formatted.replace(/\s/g, ' ')).toBe('1 000 ₸');
  });

  it('formats other currencies with uppercase code suffix', () => {
    const formattedUsd = formatPrice(50, 'USD');
    expect(formattedUsd.replace(/\s/g, ' ')).toBe('50 USD');

    const formattedEur = formatPrice(1200, 'EUR');
    expect(formattedEur.replace(/\s/g, ' ')).toBe('1 200 EUR');
  });
});

