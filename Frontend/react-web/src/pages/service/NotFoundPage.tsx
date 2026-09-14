import { Link, useNavigate } from 'react-router';

export const NotFoundPage = () => {
  const navigate = useNavigate();

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center px-4 text-center">
      {/* Декоративный бейдж */}
      <div className="relative mb-6 flex items-center justify-center">
        <span className="text-8xl font-extrabold tracking-tight text-indigo-500/15 select-none sm:text-9xl dark:text-indigo-400/20">
          404
        </span>
        <div className="absolute inset-0 flex items-center justify-center">
          <span className="rounded-full bg-indigo-50 px-4 py-1.5 text-xs font-semibold tracking-wider text-indigo-700 uppercase shadow-sm dark:bg-indigo-950/60 dark:text-indigo-300">
            Ошибка 404
          </span>
        </div>
      </div>

      {/* Заголовок и пояснение */}
      <h1 className="text-2xl font-bold tracking-tight text-[var(--text-h)] sm:text-3xl">
        Страница не найдена
      </h1>
      <p className="mt-3 max-w-md text-sm text-[var(--text)]">
        К сожалению, запрашиваемая страница не существует, была удалена или её адрес изменился.
      </p>

      {/* Быстрые действия */}
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <Link
          to="/"
          className="rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
        >
          ← В каталог событий
        </Link>

        <Link
          to="/events/topevents"
          className="rounded-xl border border-[var(--border)] bg-[var(--bg)] px-5 py-2.5 text-sm font-medium text-[var(--text)] transition hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          Топ событий
        </Link>

        <button
          type="button"
          onClick={() => {
            void navigate(-1);
          }}
          className="rounded-xl border border-[var(--border)] px-4 py-2.5 text-sm font-medium text-[var(--text)] transition hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          Назад
        </button>
      </div>
    </div>
  );
};
