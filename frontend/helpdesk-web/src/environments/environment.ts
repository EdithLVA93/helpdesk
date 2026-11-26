// Producción: la aplicación se sirve detrás del mismo dominio que la API
// (o un proxy inverso que reenvía /api). Si la API estuviera en otro dominio,
// aquí iría su URL completa y la política CORS de la API debería permitirlo.
export const environment = {
  production: true,
  apiBaseUrl: '/api',
};
