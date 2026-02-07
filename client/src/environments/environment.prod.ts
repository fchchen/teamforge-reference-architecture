export const environment = {
  production: true,
  apiUrl: 'https://teamforge-api.azurewebsites.net/api/v1',
  azure: {
    clientId: '74f5ef9e-c027-4a8e-9308-8b3ac8cdbf7a',
    authority: 'https://login.microsoftonline.com/0cb89c24-085c-4720-9a32-5cd389a23ee2',
    redirectUri: 'https://teamforge.azurewebsites.net',
    scopes: ['api://74f5ef9e-c027-4a8e-9308-8b3ac8cdbf7a/access_as_user']
  }
};
