export const environment = {
  production: true,
  apiUrl: 'https://teamforge-api.azurewebsites.net/api/v1',
  azure: {
    clientId: '<your-client-id>',
    authority: 'https://login.microsoftonline.com/<your-tenant-id>',
    redirectUri: 'https://teamforge.azurewebsites.net',
    scopes: ['api://<your-client-id>/access_as_user']
  }
};
