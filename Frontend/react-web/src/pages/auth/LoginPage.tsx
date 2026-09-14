// src/pages/LoginPage.tsx
import { type ActionFunctionArgs, redirect, useActionData, useSearchParams } from 'react-router';
import { LoginForm } from '@/features/auth/components/LoginForm';
import { loginUser } from '@/features/auth/api/authApi';
import { setToken, getSafeReturnUrl } from '@/shared/lib/auth';

interface ActionData {
  error?: string;
}

export async function action({ request }: ActionFunctionArgs): Promise<Response | ActionData> {
  const formData = await request.formData();
  const userName = formData.get('username') as string;
  const password = formData.get('password') as string;

  if (!userName || !password) {
    return { error: 'Заполните имя пользователя и пароль' } satisfies ActionData;
  }

  try {
    const { data, error } = await loginUser({ userName, password });

    if (error || !data) {
      return { error: (error as { message?: string })?.message ?? 'Неверный логин или пароль' };
    }

    setToken(data);

    const url = new URL(request.url);
    const returnUrl = getSafeReturnUrl(url.searchParams.get('returnUrl'));

    return redirect(returnUrl);
  } catch (err) {
    console.error('Login action error:', err);
    return {
      error:
        err instanceof Error
          ? err.message
          : 'Сервис временно недоступен. Проверьте интернет-соединение или попробуйте позже.',
    } satisfies ActionData;
  }
}

export const LoginPage = () => {
  const actionData = useActionData<typeof action>();
  const [searchParams] = useSearchParams();
  const returnUrl = searchParams.get('returnUrl') ?? undefined;

  return (
    <div className="flex min-h-[60vh] items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-lg border p-6 shadow-sm">
        <h1 className="mb-4 text-center text-xl font-semibold">Вход в личный кабинет</h1>
        <LoginForm error={actionData?.error} returnUrl={returnUrl} />
      </div>
    </div>
  );
};
