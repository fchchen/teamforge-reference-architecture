export const environment = {
  production: false,
  apiUrl: 'http://localhost:5210/api/v1',
  azure: {
    clientId: '<your-client-id>',
    authority: 'https://login.microsoftonline.com/<your-tenant-id>',
    redirectUri: 'http://localhost:4200',
    scopes: ['api://<your-client-id>/access_as_user']
  }
};
