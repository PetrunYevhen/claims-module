import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

interface ApiErrorBody {
  title?: string;
  errors?: Record<string, string[]>;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        auth.logout();
      }
      snackBar.open(extractMessage(error), 'Dismiss', { duration: 6000 });
      return throwError(() => error);
    })
  );
};

function extractMessage(error: HttpErrorResponse): string {
  const body = error.error as ApiErrorBody | undefined;

  if (body?.errors && Object.keys(body.errors).length > 0) {
    return Object.values(body.errors).flat().join(' ');
  }

  return body?.title ?? error.message ?? 'An unexpected error occurred.';
}
