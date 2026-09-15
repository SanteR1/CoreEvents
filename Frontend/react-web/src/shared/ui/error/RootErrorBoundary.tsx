import { isRouteErrorResponse, useRouteError, Link, useNavigate, useLocation } from 'react-router';

export const RootErrorBoundary = () => {
  const error = useRouteError();
  const navigate = useNavigate();
  const location = useLocation();

  let statusCode = 500;
  let title = 'Произошла непредвиденная ошибка';
  let message = 'Что-то пошло не так при загрузке страницы. Попробуйте обновить страницу.';
  let is401 = false;
  let is403 = false;
  let is404 = false;
  let isNetworkError = false;
  let technicalDetails: string | null = null;

  if (isRouteErrorResponse(error)) {
    statusCode = error.status;

    if (typeof error.data === 'string') {
      message = error.data;
    } else if (error.data && typeof error.data === 'object' && 'message' in error.data) {
      message = String((error.data as { message: unknown }).message);
    } else if (error.statusText) {
      message = error.statusText;
    }

    switch (error.status) {
      case 404:
        is404 = true;
        title = 'Ресурс не найден';
        message =
          message !== error.statusText
            ? message
            : 'Запрошенная страница, событие или бронирование не существуют либо были удалены.';
        break;
      case 401:
        is401 = true;
        title = 'Требуется авторизация';
        message = 'Для доступа к этому разделу необходимо войти в свою учётную запись.';
        break;
      case 403:
        is403 = true;
        title = 'Доступ запрещён';
        message = 'У вас недостаточно прав для просмотра этой страницы или выполнения действия.';
        break;
      case 500:
      case 502:
      case 503:
        title = 'Сервис временно недоступен';
        message =
          'Сервер вернул ошибку при обработке запроса. Пожалуйста, повторите попытку позже.';
        break;
      default:
        title = `Ошибка ${error.status}`;
        break;
    }
  } else if (error instanceof Error) {
    technicalDetails = error.stack ?? error.message;

    if (
      error.message.toLowerCase().includes('failed to fetch') ||
      error.message.toLowerCase().includes('networkerror') ||
      error.name === 'TypeError'
    ) {
      isNetworkError = true;
      statusCode = 0;
      title = 'Ошибка сетевого соединения';
      message =
        'Не удалось связаться с сервером. Проверьте интернет-соединение или работу микросервисов.';
    } else {
      message = error.message;
    }
  }

  const returnUrl = encodeURIComponent(location.pathname + location.search);

  return (
    <div className="mx-auto flex min-h-[70vh] max-w-2xl flex-col items-center justify-center px-4 py-12 text-center">
      {/* Иконка / статус */}
      <div className="mb-6 flex h-20 w-20 items-center justify-center rounded-2xl border border-(--border) bg-(--bg) shadow-sm">
        {is404 && <span className="text-3xl">🔍</span>}
        {is401 && <span className="text-3xl">🔒</span>}
        {is403 && <span className="text-3xl">🚫</span>}
        {isNetworkError && <span className="text-3xl">📡</span>}
        {!is404 && !is401 && !is403 && !isNetworkError && <span className="text-3xl">⚠️</span>}
      </div>

      {/* Код статуса */}
      {statusCode > 0 && (
        <span className="mb-2 rounded-full bg-red-50 px-3.5 py-1 text-xs font-semibold tracking-wider text-red-700 uppercase dark:bg-red-950/40 dark:text-red-300">
          Код ошибки: {statusCode}
        </span>
      )}

      {/* Заголовок и пояснение */}
      <h1 className="text-2xl font-bold tracking-tight text-(--text-h) sm:text-3xl">{title}</h1>
      <p className="mt-3 max-w-lg text-sm text-(--text)">{message}</p>

      {/* Действия */}
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        {/* При 401: первоочередное действие — вход в аккаунт */}
        {is401 && (
          <Link
            to={`/login?returnUrl=${returnUrl}`}
            className="rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500"
          >
            Войти в систему →
          </Link>
        )}

        {/* Кнопка повтора только для временных ошибок (сеть, 500, runtime). На 404 и 403 она бессмысленна */}
        {!is404 && !is403 && !is401 && (
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500"
          >
            Повторить попытку
          </button>
        )}

        {/* Для 404 и 403 каталог — главное действие (primary) */}
        <Link
          to="/"
          className={
            is404 || is403
              ? 'rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500'
              : 'rounded-xl border border-(--border) bg-(--bg) px-5 py-2.5 text-sm font-medium text-(--text) transition hover:bg-gray-100 dark:hover:bg-gray-800'
          }
        >
          ← В каталог событий
        </Link>

        <button
          type="button"
          onClick={() => {
            void navigate(-1);
          }}
          className="rounded-xl border border-(--border) px-4 py-2.5 text-sm font-medium text-(--text) transition hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          Назад
        </button>
      </div>

      {/* Техническая информация для разработчиков */}
      {technicalDetails && (
        <details className="mt-8 w-full text-left">
          <summary className="cursor-pointer text-xs font-medium text-(--text) hover:underline">
            Техническая информация об ошибке (для отладки)
          </summary>
          <pre className="mt-2 max-h-48 overflow-auto rounded-xl border border-(--border) bg-(--code-bg) p-4 font-mono text-xs text-(--text)">
            {technicalDetails}
          </pre>
        </details>
      )}
    </div>
  );
};
