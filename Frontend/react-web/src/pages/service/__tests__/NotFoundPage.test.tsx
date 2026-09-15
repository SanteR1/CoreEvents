import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import axe from 'axe-core';
import { NotFoundPage } from '../NotFoundPage';

const mockNavigate = vi.fn();

vi.mock('react-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('NotFoundPage Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders 404 error badge, heading and description message', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );

    expect(screen.getByText('404')).toBeInTheDocument();
    expect(screen.getByText('Ошибка 404')).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { level: 1, name: 'Страница не найдена' }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/К сожалению, запрашиваемая страница не существует/i),
    ).toBeInTheDocument();
  });

  it('renders navigation links to catalog and top events', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );

    const catalogLink = screen.getByRole('link', { name: '← В каталог событий' });
    const topEventsLink = screen.getByRole('link', { name: 'Топ событий' });

    expect(catalogLink).toBeInTheDocument();
    expect(catalogLink).toHaveAttribute('href', '/');

    expect(topEventsLink).toBeInTheDocument();
    expect(topEventsLink).toHaveAttribute('href', '/events/topevents');
  });

  it('navigates back when clicking the "Назад" button', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );

    const backButton = screen.getByRole('button', { name: 'Назад' });
    await user.click(backButton);

    expect(mockNavigate).toHaveBeenCalledTimes(1);
    expect(mockNavigate).toHaveBeenCalledWith(-1);
  });

  it('satisfies a11y accessibility audit', async () => {
    const { container } = render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );

    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    });
    expect(results.violations).toEqual([]);
  });
});
