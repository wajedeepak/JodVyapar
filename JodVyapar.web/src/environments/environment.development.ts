// `ng serve` forwards /api to the .NET API via proxy.conf.json, so a relative URL works in dev.
export const environment = {
  production: false,
  apiBaseUrl: '/api/v1',
};
