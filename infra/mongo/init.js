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
 *   Root:      root / root          (administração)
 *   Aplicação: cashflow / cashflow  (acesso somente ao cashflow_read)
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

print('✅ MongoDB inicializado: banco cashflow_read, usuário cashflow e índices criados.');

