# Gateway

API Gateway do projeto Arch Challenge, construído com **ASP.NET Core 8** e **Ocelot**. Centraliza o roteamento, autenticação JWT (Keycloak) e rate limiting para os serviços downstream.

---

## Rotas disponíveis


| Prefixo upstream       | Serviço downstream | Métodos                | Roles exigidas         |
| ---------------------- | ------------------ | ---------------------- | ---------------------- |
| `/cashflow/v1/{tudo}`  | CashFlow API       | GET, POST, PUT, DELETE | `comerciante`, `admin` |
| `/dashboard/v1/{tudo}` | Dashboard API      | GET                    | `gestor`, `admin`      |


Todas as rotas exigem um **token JWT Bearer** válido emitido pelo Keycloak.

---

## Autenticação

O gateway valida tokens JWT contra o Keycloak. As configurações relevantes ficam em `appsettings.json`:

```json
"Keycloak": {
  "Authority": "<keycloak-url>/realms/<realm>",
  "ValidAudiences": ["cashflow-api", "dashboard-api", "account"],
  "RequireHttpsMetadata": false
}
```

A claim `roles` do token é mapeada automaticamente via `KeycloakRolesClaimsTransformation` e validada por `CommaSeparatedRolesClaimsAuthorizer`, que suporta múltiplos roles separados por vírgula.

---

## Rate Limiting

O rate limiting é implementado nativamente pelo Ocelot, com limites distintos por rota. O cliente é identificado pelo IP de origem; opcionalmente, é possível enviar o header `X-ClientId` para identificação explícita.

### Limites por rota


| Rota             | Limite      | Janela | Tempo de retry |
| ---------------- | ----------- | ------ | -------------- |
| `/cashflow/v1/`  | 60 requests | 1 min  | 60 s           |
| `/dashboard/v1/` | 30 requests | 1 min  | 60 s           |


### Comportamento ao exceder o limite

- **HTTP status:** `429 Too Many Requests`
- **Mensagem:** `Too Many Requests — limite de requisições atingido. Tente novamente em instantes.`
- **Headers de controle expostos** (retornados em todas as respostas):
  - `X-Rate-Limit-Limit` — limite total de requests no período
  - `X-Rate-Limit-Remaining` — requests restantes na janela atual
  - `X-Rate-Limit-Reset` — timestamp (Unix) de reset da janela

### Configuração (ocelot.json)

Cada rota contém um bloco `RateLimitOptions`:

```json
"RateLimitOptions": {
  "ClientWhitelist": [],
  "EnableRateLimiting": true,
  "Period": "1m",
  "PeriodTimespan": 60,
  "Limit": 60
}
```


| Campo                | Descrição                                                           |
| -------------------- | ------------------------------------------------------------------- |
| `ClientWhitelist`    | Lista de IPs ou ClientIds isentos do rate limiting                  |
| `EnableRateLimiting` | Habilita ou desabilita o rate limiting para a rota                  |
| `Period`             | Janela de tempo (`1s`, `1m`, `1h`, `1d`)                            |
| `PeriodTimespan`     | Tempo em segundos que o cliente deve aguardar após exceder o limite |
| `Limit`              | Número máximo de requisições permitidas no período                  |


A configuração global, em `GlobalConfiguration.RateLimitOptions`, controla o comportamento da resposta:

```json
"RateLimitOptions": {
  "DisableRateLimitHeaders": false,
  "QuotaExceededMessage": "Too Many Requests — ...",
  "HttpStatusCode": 429,
  "ClientIdHeader": "X-ClientId"
}
```

---

## Executando localmente

```bash
dotnet run
```

Por padrão, o gateway sobe em `http://localhost:5000`. O ambiente de desenvolvimento usa `ocelot.Development.json`, que aponta para `localhost:5001` (CashFlow API) e `localhost:5002` (Dashboard API).

Para forçar o uso dos hosts de produção (Docker), defina `Gateway:UseLocalDownstreamHosts=false` nas variáveis de ambiente.

### Swagger UI

Disponível em desenvolvimento em:

```
http://localhost:5000/swagger
```

Agrega a documentação das APIs downstream via `MMLib.SwaggerForOcelot`.

---

## Docker

```bash
docker build -t arch-challenge-gateway .
docker run -p 5000:8080 arch-challenge-gateway
```

---

## Estrutura do projeto

```
gateway/
├── Authorization/
│   └── CommaSeparatedRolesClaimsAuthorizer.cs  # Validação de roles com vírgula
├── Security/
│   └── KeycloakRolesClaimsTransformation.cs    # Mapeamento de claims do Keycloak
├── Program.cs                                   # Pipeline HTTP e configuração de serviços
├── ocelot.json                                  # Rotas (produção / Docker)
├── ocelot.Development.json                      # Rotas (desenvolvimento local)
└── appsettings.json                             # Keycloak, CORS, logging
```

