export const environment = {
  production: false,
  apiUrl: 'http://localhost:5210/api/v1',
  azure: {
    clientId: '74f5ef9e-c027-4a8e-9308-8b3ac8cdbf7a',
    authority: 'https://login.microsoftonline.com/0cb89c24-085c-4720-9a32-5cd389a23ee2',
    redirectUri: 'http://localhost:4200',
    scopes: ['api://74f5ef9e-c027-4a8e-9308-8b3ac8cdbf7a/access_as_user']
  }
};
