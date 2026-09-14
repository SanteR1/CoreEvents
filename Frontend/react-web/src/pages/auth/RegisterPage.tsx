// src/pages/RegisterPage.tsx
import { type ActionFunctionArgs, redirect, useActionData, useSearchParams } from 'react-router';
import { RegisterForm } from '@/features/auth/components/RegisterForm';
import { registerUser, loginUser } from '@/features/auth/api/authApi';
import { setToken } from '@/shared/lib/auth/sessionStore';

interface ActionData {
  error?: string;
}

// Защита от внешних редиректов
function getSafeReturnUrl(target: string | null): string {
  if (!target || !target.startsWith('/') || target.startsWith('//')) {
    return '/';
  }
  return target;
}

export async function action({ request }: ActionFunctionArgs): Promise<Response | ActionData> {
  const formData = await request.formData();
  const userName = formData.get('username') as string;
  const password = formData.get('password') as string;

  if (!userName || !password) {
    return { error: 'Заполните имя пользователя и пароль' } satisfies ActionData;
  }

  const url = new URL(request.url);
  const returnUrl = getSafeReturnUrl(url.searchParams.get('returnUrl'));

  try {
    // 1. Регистрация нового пользователя
    const regResult = await registerUser({ userName, password });

    // Проверяем статус ответа (204 NoContent, 200 OK или 201 Created)
    if (
      regResult.error ||
      (regResult.status !== 204 && regResult.status !== 200 && regResult.status !== 201)
    ) {
      const errObj = regResult.error as
        { detail?: string; title?: string; message?: string } | undefined;
      return {
        error:
          errObj?.detail ??
          errObj?.title ??
          errObj?.message ??
          'Ошибка регистрации. Возможно, пользователь с таким именем уже существует.',
      } satisfies ActionData;
    }

    // 2. Автоматический вход после успешной регистрации
    const loginResult = await loginUser({ userName, password });

    if (loginResult.error || !loginResult.data) {
      // Если регистрация прошла, но логин не удался (например, временный сбой) — перенаправляем на логин
      return redirect(returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : '/login');
    }

    // 3. Сохраняем токен в хранилище сессии
    setToken(loginResult.data);

    // 4. Перенаправляем на целевую страницу
    return redirect(returnUrl);
  } catch (err) {
    console.error('Register action error:', err);
    return {
      error:
        err instanceof Error
          ? err.message
          : 'Сервис временно недоступен. Проверьте интернет-соединение или попробуйте позже.',
    } satisfies ActionData;
  }
}

export const RegisterPage = () => {
  const actionData = useActionData<typeof action>();
  const [searchParams] = useSearchParams();
  const returnUrl = searchParams.get('returnUrl') ?? undefined;

  return (
    <div className="flex min-h-[60vh] items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-lg border border-[var(--border)] bg-[var(--bg)] p-6 shadow-sm">
        <h1 className="mb-4 text-center text-xl font-semibold text-[var(--text-h)]">Регистрация</h1>
        <RegisterForm error={actionData?.error} returnUrl={returnUrl} />
      </div>
    </div>
  );
};
