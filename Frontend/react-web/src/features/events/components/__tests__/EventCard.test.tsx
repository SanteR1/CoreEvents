import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { EventCard } from '../EventCard';
import { checkA11y } from '@/shared/lib/test/axe';
import type { EventResponse } from '@/features/events/api/eventsApi';

function renderEventCard(event: EventResponse) {
  return render(
    <MemoryRouter>
      <EventCard event={event} />
    </MemoryRouter>,
  );
}

describe('EventCard', () => {
  const baseEvent: EventResponse = {
    id: 'ev-test-1',
    title: 'Конференция Tech',
    description: 'Ежегодная IT-конференция для разработчиков',
    startAt: new Date('2026-10-15T09:00:00Z'),
    endAt: new Date('2026-10-15T18:00:00Z'),
    totalSeats: 100,
    availableSeats: 50,
  };

  describe('Seat Availability Statuses & Boundaries', () => {
    it('renders "Мест нет" badge and disabled "Распродано" button when availableSeats = 0', () => {
      renderEventCard({ ...baseEvent, availableSeats: 0 });

      expect(screen.getByText('Мест нет')).toBeInTheDocument();
      expect(screen.getByText('Всего: 100')).toBeInTheDocument();

      const soldOutBtn = screen.getByRole('button', { name: 'Распродано' });
      expect(soldOutBtn).toBeDisabled();
      expect(screen.queryByRole('link', { name: 'Забронировать' })).not.toBeInTheDocument();
    });

    it('renders "Мест нет" and disabled "Распродано" when availableSeats < 0 (defensive)', () => {
      renderEventCard({ ...baseEvent, availableSeats: -2 });

      expect(screen.getByText('Мест нет')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Распродано' })).toBeDisabled();
      expect(screen.queryByRole('link', { name: 'Забронировать' })).not.toBeInTheDocument();
    });

    it('renders "Осталось мест: 1" and "Забронировать" link when availableSeats = 1', () => {
      renderEventCard({ ...baseEvent, availableSeats: 1 });

      expect(screen.getByText('Осталось мест: 1')).toBeInTheDocument();
      const bookLink = screen.getByRole('link', { name: 'Забронировать' });
      expect(bookLink).toHaveAttribute('href', `/bookings/create/${baseEvent.id}`);
      expect(screen.queryByRole('button', { name: 'Распродано' })).not.toBeInTheDocument();
    });

    it('renders "Осталось мест: 5" and "Забронировать" link when availableSeats = 5 (upper fewSeats boundary)', () => {
      renderEventCard({ ...baseEvent, availableSeats: 5 });

      expect(screen.getByText('Осталось мест: 5')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Забронировать' })).toBeInTheDocument();
    });

    it('renders "Свободно мест: 6 из 100" when availableSeats = 6 (standard available state)', () => {
      renderEventCard({ ...baseEvent, availableSeats: 6 });

      expect(screen.getByText('Свободно мест: 6 из 100')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Забронировать' })).toBeInTheDocument();
    });

    it('renders "Свободно мест: 100 из 100" when all seats are available', () => {
      renderEventCard({ ...baseEvent, availableSeats: 100, totalSeats: 100 });

      expect(screen.getByText('Свободно мест: 100 из 100')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Забронировать' })).toBeInTheDocument();
    });
  });

  describe('Content Fallbacks and Rendering', () => {
    it('renders fallback "Без названия" when title is empty', () => {
      renderEventCard({ ...baseEvent, title: '' });

      const titleLink = screen.getByRole('link', { name: 'Без названия' });
      expect(titleLink).toHaveAttribute('href', `/events/${baseEvent.id}`);
    });

    it('renders fallback "Описание отсутствует" when description is null or empty', () => {
      renderEventCard({
        ...baseEvent,
        description: null,
      });

      expect(screen.getByText('Описание отсутствует')).toBeInTheDocument();
    });

    it('renders provided description when available', () => {
      renderEventCard(baseEvent);

      expect(screen.getByText(baseEvent.description!)).toBeInTheDocument();
    });
  });

  describe('Dates Formatting & Fallbacks', () => {
    it('formats start and end dates with Russian locale', () => {
      renderEventCard(baseEvent);

      expect(screen.getByText('Начало:')).toBeInTheDocument();
      expect(screen.getByText('Окончание:')).toBeInTheDocument();
      // Should format day, short month, year, time
      expect(screen.getAllByText(/2026/)).toHaveLength(2);
    });

    it('handles date parsing exception defensively with "Дата не указана"', () => {
      const corruptDate = {
        [Symbol.toPrimitive]() {
          throw new Error('Date parsing error');
        },
      } as unknown as Date;

      renderEventCard({
        ...baseEvent,
        startAt: corruptDate,
        endAt: corruptDate,
      });

      const fallbacks = screen.getAllByText('Дата не указана');
      expect(fallbacks).toHaveLength(2);
    });
  });

  describe('Action Links and Navigation', () => {
    it('links to /events/:id via "Подробнее" and title', () => {
      renderEventCard(baseEvent);

      const detailsLink = screen.getByRole('link', { name: 'Подробнее' });
      expect(detailsLink).toHaveAttribute('href', `/events/${baseEvent.id}`);

      const titleLink = screen.getByRole('link', { name: baseEvent.title });
      expect(titleLink).toHaveAttribute('href', `/events/${baseEvent.id}`);
    });
  });

  describe('Accessibility', () => {
    it('satisfies WCAG accessibility rules for available event', async () => {
      const { container } = renderEventCard(baseEvent);
      await expect(checkA11y(container)).resolves.toEqual([]);
    });

    it('satisfies WCAG accessibility rules for sold out event', async () => {
      const { container } = renderEventCard({ ...baseEvent, availableSeats: 0 });
      await expect(checkA11y(container)).resolves.toEqual([]);
    });
  });
});
