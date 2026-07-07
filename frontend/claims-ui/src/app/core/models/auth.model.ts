export type UserRole = 'handler' | 'supervisor' | 'manager';

export interface MockUserSummary {
  username: string;
  name: string;
  role: UserRole;
}

export interface LoginResponse {
  token: string;
  userId: string;
  name: string;
  role: UserRole;
}

export interface CurrentUser {
  userId: string;
  name: string;
  role: UserRole;
  token: string;
}
