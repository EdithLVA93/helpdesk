/** UserResponse de la API. */
export interface User {
  id: number;
  name: string;
  email: string;
  /** ISO 8601 en UTC (termina en "Z"). */
  createdAt: string;
}

/** CreateUserRequest de la API (POST /api/users). */
export interface CreateUserRequest {
  name: string;
  email: string;
}

/** Límites del backend (User.NameMaxLength / User.EmailMaxLength). */
export const USER_LIMITS = {
  nameMaxLength: 100,
  emailMaxLength: 254,
} as const;
