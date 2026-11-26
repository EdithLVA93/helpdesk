// Proxy del servidor de desarrollo (ng serve).
//
// El navegador solo habla con http://localhost:4200; las peticiones a /api las
// reenvía el servidor de Angular a la API .NET. Así, en desarrollo:
//   - no hay peticiones entre orígenes distintos (no interviene CORS);
//   - no hace falta confiar en el certificado HTTPS de desarrollo de .NET
//     (secure: false acepta el certificado autofirmado de localhost).
//
// Por defecto apunta al perfil "https" de la API (launchSettings.json).
// Para usar otra dirección:  $env:HELPDESK_API_URL = "http://localhost:5080"; npm start
const target = process.env['HELPDESK_API_URL'] ?? 'https://localhost:7080';

export default {
  '/api': {
    target,
    secure: false,
    changeOrigin: true,
  },
};
