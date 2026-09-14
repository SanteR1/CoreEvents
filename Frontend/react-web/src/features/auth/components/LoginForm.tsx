import { Form, useNavigation, Link } from 'react-router';

interface LoginFormProps {
  error?: string;
  returnUrl?: string | null;
}

export const LoginForm = ({ error, returnUrl }: LoginFormProps) => {
  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  return (
    <Form
      method="post"
      action={returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : '/login'}
      className="w-full space-y-4"
    >
      {error && (
        <div className="rounded border border-red-200 bg-red-50 p-2 text-sm text-red-600">
          {error}
        </div>
      )}

      <div>
        <label htmlFor="login-username" className="mb-1 block text-sm font-medium">
          Имя пользователя
        </label>
        <input
          id="login-username"
          type="text"
          name="username"
          required
          autoComplete="username"
          placeholder="Имя пользователя"
          className="w-full rounded border px-3 py-1.5 outline-none focus:ring-1 focus:ring-blue-500"
        />
      </div>

      <div>
        <label htmlFor="login-password" className="mb-1 block text-sm font-medium">
          Пароль
        </label>
        <input
          id="login-password"
          type="password"
          name="password"
          required
          autoComplete="current-password"
          placeholder="Пароль"
          className="w-full rounded border px-3 py-1.5 outline-none focus:ring-1 focus:ring-blue-500"
        />
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full cursor-pointer rounded bg-blue-600 py-2 font-medium text-white transition-colors hover:bg-blue-700 disabled:opacity-50"
      >
        {isSubmitting ? 'Вход...' : 'Войти'}
      </button>

      <div className="user-login_footer flex w-full justify-between">
        <Link
          to="/recovery"
          className="font-medium text-blue-600 transition-colors hover:underline dark:text-blue-400"
        >
          Забыли пароль?
        </Link>
        <Link
          to={returnUrl ? `/register?returnUrl=${encodeURIComponent(returnUrl)}` : '/register'}
          className="font-medium text-blue-600 transition-colors hover:underline dark:text-blue-400"
        >
          Регистрация
        </Link>
      </div>
    </Form>
  );
};
