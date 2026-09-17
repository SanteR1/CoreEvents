import { NavLink, Link, useNavigation, useNavigate } from 'react-router';
import { clearToken, useIsAuthenticated } from '@/shared/lib/auth';
import { ThemeToggle } from '@/shared/ui/theme/ThemeToggle';

export const Header = () => {
  const navigation = useNavigation();
  const navigate = useNavigate();
  const isLoading = navigation.state === 'loading';
  const isAuthenticated = useIsAuthenticated();

  const handleLogout = () => {
    clearToken();
    void navigate('/login');
  };

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `transition-colors ${
      isActive ? 'font-semibold text-(--accent)' : 'text-(--text-h) hover:text-(--accent)'
    }`;

  return (
    <header className="border-b border-(--border) px-6 py-4">
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
          {isLoading && <span className="animate-pulse text-xs text-(--accent)">Загрузка...</span>}

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
              <Link to="/login" className="text-(--text-h) transition-colors hover:text-(--accent)">
                Вход
              </Link>
            </div>
          )}
        </div>
      </nav>
    </header>
  );
};
