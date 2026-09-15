// src/shared/api/applyFieldErrors.ts

// На будущие, что бы не забыть

// import type { FieldValues, UseFormSetError, Path } from 'react-hook-form';
// import type { FieldErrors } from './errors';

// export function applyFieldErrors<T extends FieldValues>(
//   fieldErrors: FieldErrors | undefined,
//   setError: UseFormSetError<T>,
// ) {
//   if (!fieldErrors) return;

//   Object.entries(fieldErrors).forEach(([key, messages]) => {
//     setError(key as Path<T>, {
//       type: 'server',
//       message: messages[0], // либо messages.join(', ') если нужно показать все
//     });
//   });
// }
