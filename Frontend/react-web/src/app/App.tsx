import { useSyncExternalStore } from 'react';
import { Outlet, NavLink, Link, useNavigation, useNavigate } from 'react-router';
import { getToken, clearToken, subscribe } from '@/shared/lib/auth/sessionStore';
import { ThemeProvider } from '@/app/providers/ThemeProvider';
import { ThemeToggle } from '@/shared/ui/theme/ThemeToggle';

export default function App() {
  const navigation = useNavigation();
  const navigate = useNavigate();
  const isLoading = navigation.state === 'loading';

  const token = useSyncExternalStore(subscribe, getToken, () => null);
  const isAuthenticated = Boolean(token);

  const handleLogout = () => {
    clearToken();
    void navigate('/login');
  };

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `transition-colors ${
      isActive
        ? 'font-semibold text-[var(--accent)]'
        : 'text-[var(--text-h)] hover:text-[var(--accent)]'
    }`;

  return (
    <ThemeProvider>
      <div className="min-h-screen bg-[var(--bg)] text-[var(--text)] transition-colors duration-200">
        <header className="border-b border-[var(--border)] px-6 py-4">
          <nav className="mx-auto flex max-w-7xl items-center gap-6 text-sm font-medium">
            {/* 1. Публичные ссылки */}
            <NavLink to="/" end className={navLinkClass}>
              Главная
            </NavLink>
            <NavLink to="/events/topevents" className={navLinkClass}>
              Топовые события
            </NavLink>

            {/* 2. Приватные ссылки */}
            {isAuthenticated && (
              <NavLink to="/events/create" className={navLinkClass}>
                Создать событие
              </NavLink>
            )}

            {/* 3. Блок профиля, индикатор загрузки и переключатель тем */}
            <div className="ml-auto flex items-center gap-4">
              {isLoading && (
                <span className="animate-pulse text-xs text-[var(--accent)]">Загрузка...</span>
              )}

              {/* Переключатель Light / System / Dark */}
              <ThemeToggle />

              {isAuthenticated ? (
                <button
                  type="button"
                  onClick={handleLogout}
                  className="text-sm font-medium text-red-600 transition-colors hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                >
                  Выйти
                </button>
              ) : (
                <div className="flex items-center gap-4">
                  <Link
                    to="/login"
                    className="text-[var(--text-h)] transition-colors hover:text-[var(--accent)]"
                  >
                    Вход
                  </Link>
                </div>
              )}
            </div>
          </nav>
        </header>

        <main
          className="mx-auto max-w-7xl p-6 transition-opacity duration-200"
          style={{ opacity: isLoading ? 0.6 : 1 }}
        >
          <Outlet />
        </main>
      </div>
    </ThemeProvider>
  );
}
