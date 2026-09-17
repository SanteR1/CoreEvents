// src/features/auth/api/authApi.ts
import { usersClient } from '@/shared/api';
import type { UserSchema } from '@/shared/api';
import { isProblemDetails } from '@/shared/api/errors';

type LoginDto = UserSchema<'UserLoginDto'>;
type RegisterDto = UserSchema<'UserRegisterDto'>;

export async function registerUser(credentials: RegisterDto) {
  const { data, error, response } = await usersClient.POST('/Auth/register', {
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
  const { data, error, response } = await usersClient.POST('/Auth/login', {
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
