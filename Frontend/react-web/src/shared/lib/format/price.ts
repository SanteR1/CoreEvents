export function formatPrice(price: number, currency = 'KZT'): string {
  if (price <= 0) {
    return 'Бесплатно';
  }

  const formatted = price.toLocaleString('ru-RU');
  if (currency.toUpperCase() === 'KZT') {
    return `${formatted} ₸`;
  }

  return `${formatted} ${currency}`;
}

