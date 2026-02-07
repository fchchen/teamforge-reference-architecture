import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

// Detect if this window is an MSAL popup returning with an auth response.
// MSAL v5 uses BroadcastChannel to pass the auth code back to the parent.
// The popup must call broadcastResponseToMainFrame() instead of bootstrapping the app.
const hash = window.location.hash;
const isAuthResponse = hash.includes('code=') || hash.includes('error=');
const isPopupWindow = (window.opener != null && window.opener !== window) ||
  !!window.name?.startsWith('msal.');

if (isAuthResponse && isPopupWindow) {
  import('@azure/msal-browser/redirect-bridge').then(({ broadcastResponseToMainFrame }) => {
    broadcastResponseToMainFrame().catch(() => {
      window.close();
    });
  });
} else {
  bootstrapApplication(AppComponent, appConfig)
    .catch((err) => console.error(err));
}
