# Convenções de Nomenclatura — Banco de Dados Relacional

## Visão geral

Este documento define o padrão de nomenclatura para todos os objetos de banco de dados relacional criados pelos serviços .NET do sistema. As convenções aqui descritas são **obrigatórias** e se aplicam independente da abordagem utilizada (EF Core Migrations, SQL puro, scripts de inicialização, etc.).

**Regras gerais:**
- Todos os nomes devem estar em **MAIÚSCULAS**
- Todos os nomes devem ser escritos em **inglês**
- Separador de palavras: underline (`_`)

---

## Estruturas

### Tabelas

**Padrão:** `TB_{nome no singular}`

| Exemplo | Descrição |
|---|---|
| `TB_USER` | Tabela de usuários |
| `TB_TRANSACTION` | Tabela de transações |
| `TB_PAYMENT_METHOD` | Tabela de métodos de pagamento |

> O nome deve ser sempre no **singular**, representando uma entidade única.

### Índices

**Padrão:** `IX_{nome da coluna}`

| Exemplo | Descrição |
|---|---|
| `IX_DS_EMAIL` | Índice sobre a coluna de e-mail |
| `IX_DT_CREATED_AT` | Índice sobre a coluna de data de criação |
| `IX_ID_USER` | Índice sobre a chave estrangeira de usuário |

> Para índices compostos, concatene os nomes das colunas: `IX_{coluna_1}_{coluna_2}`.

---

## Constraints

| Tipo | Prefixo | Exemplo |
|---|---|---|
| Chave primária | `PK_` | `PK_TB_USER` |
| Chave estrangeira | `FK_` | `FK_TB_TRANSACTION_USER` |

> A convenção completa para constraints inclui o nome da tabela após o prefixo para facilitar identificação em mensagens de erro e logs do banco.

---

## Colunas

### Identificadores (IDs)

| Finalidade | Padrão | Exemplo |
|---|---|---|
| Chave primária da própria tabela | `ID` | `ID` |
| Chave estrangeira para outra tabela | `ID_{nome da tabela sem o prefixo TB_}` | `ID_USER`, `ID_PAYMENT_METHOD` |

### Textos

| Finalidade | Padrão | Exemplo |
|---|---|---|
| Texto descritivo (campo livre, observações) | `DS_{contexto}` | `DS_TRANSACTION`, `DS_NOTES` |
| Nome de uma entidade (nome próprio, título) | `NM_{contexto}` | `NM_USER`, `NM_CATEGORY` |

> `DS_` é usado para textos sem estrutura definida (ex: descrição, observação).  
> `NM_` é reservado para nomes que identificam uma entidade (ex: nome do usuário, nome do produto).

### Números

| Finalidade | Padrão | Exemplo |
|---|---|---|
| Valor numérico geral (quantidade, código, sequência) | `NR_{contexto}` | `NR_INSTALLMENTS`, `NR_DOCUMENT` |
| Valor monetário (moeda, preço, saldo) | `VL_{contexto}` | `VL_AMOUNT`, `VL_BALANCE`, `VL_FEE` |

> `VL_` é exclusivo para valores que representam **unidade monetária**. Para qualquer outro número, use `NR_`.

### Status, booleanos e enumerações

**Padrão:** `ST_{contexto}`

| Exemplo | Descrição |
|---|---|
| `ST_ACTIVE` | Flag booleano de ativo/inativo |
| `ST_TRANSACTION` | Status de uma transação (enum: PENDING, APPROVED, CANCELLED) |
| `ST_PAYMENT` | Status de pagamento |

> Tanto campos booleanos (`true/false`) quanto enumerações e representações de estado utilizam o prefixo `ST_`.

### Datas

**Padrão:** `DT_{contexto}`

| Exemplo | Descrição |
|---|---|
| `DT_CREATED_AT` | Data/hora de criação do registro |
| `DT_UPDATED_AT` | Data/hora da última atualização |
| `DT_TRANSACTION` | Data da transação |
| `DT_EXPIRATION` | Data de vencimento |

> O prefixo `DT_` cobre tanto tipos `DATE` quanto `TIMESTAMP`/`DATETIME`.

---

## Referência rápida

| Categoria | Prefixo | Aplicação |
|---|---|---|
| Tabela | `TB_` | Nome no singular |
| Índice | `IX_` | Nome da(s) coluna(s) indexada(s) |
| Chave primária | `PK_` | Constraint da chave primária |
| Chave estrangeira | `FK_` | Constraint da chave estrangeira |
| ID — chave primária | `ID` | Sem prefixo adicional |
| ID — chave estrangeira | `ID_` | Seguido do nome da tabela sem `TB_` |
| Texto descritivo | `DS_` | Campos livres, observações |
| Nome de entidade | `NM_` | Nomes próprios, títulos |
| Número geral | `NR_` | Quantidades, códigos, sequências |
| Valor monetário | `VL_` | Preços, saldos, taxas |
| Status / booleano / enum | `ST_` | Flags e estados |
| Data / hora | `DT_` | Campos DATE e TIMESTAMP |

---

## Exemplo prático

```sql
CREATE TABLE TB_TRANSACTION (
    ID              UUID          NOT NULL,
    ID_USER         UUID          NOT NULL,
    ID_PAYMENT_METHOD UUID        NOT NULL,
    DS_TRANSACTION  VARCHAR(255),
    VL_AMOUNT       NUMERIC(15,2) NOT NULL,
    NR_INSTALLMENTS INTEGER       NOT NULL DEFAULT 1,
    ST_TRANSACTION  VARCHAR(20)   NOT NULL,
    DT_TRANSACTION  DATE          NOT NULL,
    DT_CREATED_AT   TIMESTAMP     NOT NULL DEFAULT NOW(),
    DT_UPDATED_AT   TIMESTAMP,

    CONSTRAINT PK_TB_TRANSACTION PRIMARY KEY (ID),
    CONSTRAINT FK_TB_TRANSACTION_USER FOREIGN KEY (ID_USER) REFERENCES TB_USER (ID),
    CONSTRAINT FK_TB_TRANSACTION_PAYMENT_METHOD FOREIGN KEY (ID_PAYMENT_METHOD) REFERENCES TB_PAYMENT_METHOD (ID)
);

CREATE INDEX IX_ID_USER ON TB_TRANSACTION (ID_USER);
CREATE INDEX IX_DT_TRANSACTION ON TB_TRANSACTION (DT_TRANSACTION);
CREATE INDEX IX_ST_TRANSACTION ON TB_TRANSACTION (ST_TRANSACTION);
```
