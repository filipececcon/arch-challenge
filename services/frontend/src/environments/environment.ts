/** Produção — ajuste URLs conforme o deploy. */
export const environment = {
  production: true,
  keycloakUrl: 'http://localhost:8080',
  realm: 'cashflow',
  clientId: 'cashflow-frontend',
  appOrigin: 'http://localhost:4200',
  gatewayUrl: 'http://localhost:5000',
}
