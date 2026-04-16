/**
 * init.js — Script de inicialização do MongoDB
 *
 * Executado automaticamente pelo container mongo:7.0 na primeira inicialização
 * (via /docker-entrypoint-initdb.d/).
 *
 * Responsabilidades:
 *   1. Criar o banco de dados `cashflow_read` (read model / CQRS)
 *   2. Criar o usuário de aplicação `cashflow` com acesso restrito ao banco
 *   3. Criar a coleção `transactions` com índices otimizados para consulta
 *
 * Credenciais (ambiente local / desenvolvimento):
 *   Root:       root / root           (administração)
 *   CashFlow:   cashflow / cashflow  (acesso somente ao cashflow_read)
 *   Dashboard:  dashboard / dashboard (acesso somente ao dashboard_read)
 *
 * Nota: em produção, use variáveis de ambiente via secrets manager e
 *       não exponha credenciais neste arquivo.
 */

// Muda para o banco de administração para criar o usuário da aplicação
db = db.getSiblingDB('admin');

db.createUser({
  user: 'cashflow',
  pwd:  'cashflow',
  roles: [
    {
      role: 'readWrite',
      db:   'cashflow_read'
    }
  ]
});

db.createUser({
  user: 'dashboard',
  pwd:  'dashboard',
  roles: [
    {
      role: 'readWrite',
      db:   'dashboard_read'
    }
  ]
});

// Muda para o banco da aplicação e configura coleção + índices
db = db.getSiblingDB('cashflow_read');

// Cria a coleção explicitamente (opcional, mas deixa o schema visível)
db.createCollection('transactions');

/**
 * Índices da coleção transactions
 *
 * _id:       ID da transação (string UUID) — upsert idempotente pelo OutboxWorker
 * type:      filtros por tipo (CREDIT / DEBIT)
 * createdAt: ordenação cronológica e filtros por data
 */
db.transactions.createIndex({ type:      1 }, { name: 'idx_type'      });
db.transactions.createIndex({ createdAt: -1 }, { name: 'idx_created_at' });
db.transactions.createIndex(
  { type: 1, createdAt: -1 },
  { name: 'idx_type_created_at' }
);

// ── Dashboard: read model (consolidados + idempotência de eventos) ─────────────
db = db.getSiblingDB('dashboard_read');

db.createCollection('daily_consolidations');
db.createCollection('processed_integration_events');

print('✅ MongoDB inicializado: cashflow_read + dashboard_read, usuários e índices criados.');

