import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { httpErrorInterceptor, loadingInterceptor } from './core/interceptors/http-error.interceptor';
import { MsalService } from './core/services/msal.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([authInterceptor, httpErrorInterceptor, loadingInterceptor])
    ),
    provideAnimationsAsync(),
    {
      provide: APP_INITIALIZER,
      useFactory: (msalService: MsalService) => () => msalService.initialize(),
      deps: [MsalService],
      multi: true
    }
  ]
};
