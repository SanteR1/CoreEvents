// src/features/auth/api/authApi.ts
import { usersClient } from '@/shared/api';
import type { UserSchema } from '@/shared/api';
import { isProblemDetails } from '@/shared/api/errors';
import type { User } from '@/shared/lib/auth/user';
import { clearUser } from '@/shared/lib/auth/sessionStore';

type LoginDto = UserSchema<'UserLoginDto'>;
type RegisterDto = UserSchema<'UserRegisterDto'>;
export type UserResponse = UserSchema<'UserResponseDto'>;

export async function registerUser(credentials: RegisterDto) {
  const { data, error, response } = await usersClient.POST('/v1/auth/register', {
    body: credentials,
  });

  if (isProblemDetails(data)) {
    return {
      data: undefined,
      error: data,
      status: typeof data.status === 'number' ? data.status : 400,
    };
  }

  return { data, error, status: response.status };
}

export async function loginUser(credentials: LoginDto) {
  const { data, error, response } = await usersClient.POST('/v1/auth/login', {
    body: credentials,
  });

  if (isProblemDetails(data)) {
    return {
      data: undefined,
      error: data,
      status: typeof data.status === 'number' ? data.status : 400,
    };
  }

  return { data, error, status: response.status };
}

export async function getCurrentUser(): Promise<User | null> {
  try {
    const { data, error, response } = await usersClient.GET('/v1/users/me');
    const isProblem = isProblemDetails(data);

    if (!response.ok || error || !data || isProblem) {
      return null;
    }

    const user = data as User;
    return {
      id: user.id,
      userName: user.userName,
      role: user.role,
    };
  } catch {
    return null;
  }
}

export async function refreshSession(): Promise<boolean> {
  try {
    const { response } = await usersClient.POST('/v1/auth/refresh');
    return response.ok;
  } catch {
    return false;
  }
}

export async function logoutUser(): Promise<void> {
  try {
    await usersClient.POST('/v1/auth/logout');
  } catch {
    // Игнорируем сетевые ошибки при вызове logout на клиенте
  } finally {
    clearUser();
  }
}
