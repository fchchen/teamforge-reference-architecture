import { Injectable } from '@angular/core';
import { PublicClientApplication, AuthenticationResult } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MsalService {
  private msalInstance: PublicClientApplication | null = null;

  async initialize(): Promise<void> {
    this.msalInstance = new PublicClientApplication({
      auth: {
        clientId: environment.azure.clientId,
        authority: environment.azure.authority,
        redirectUri: environment.azure.redirectUri
      },
      cache: {
        cacheLocation: 'sessionStorage',
        storeAuthStateInCookie: false
      }
    });
    await this.msalInstance.initialize();
  }

  async loginPopup(): Promise<string> {
    if (!this.msalInstance) {
      throw new Error('MSAL not initialized');
    }

    const result: AuthenticationResult = await this.msalInstance.loginPopup({
      scopes: environment.azure.scopes
    });

    return result.accessToken;
  }

  async acquireTokenSilent(): Promise<string> {
    if (!this.msalInstance) {
      throw new Error('MSAL not initialized');
    }

    const accounts = this.msalInstance.getAllAccounts();
    if (accounts.length === 0) {
      throw new Error('No accounts found');
    }

    const result = await this.msalInstance.acquireTokenSilent({
      scopes: environment.azure.scopes,
      account: accounts[0]
    });

    return result.accessToken;
  }

  async logout(): Promise<void> {
    if (!this.msalInstance) return;
    await this.msalInstance.logoutPopup();
  }
}
