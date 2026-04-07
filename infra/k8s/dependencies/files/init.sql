-- Criação dos bancos de dados separados por serviço (Database per Service pattern)
-- Referência: ADR-006
-- Fonte canônica: infra/postgres/init.sql (manter alinhado)

CREATE DATABASE cashflow_db;
CREATE DATABASE dashboard_db;
CREATE DATABASE keycloak_db;
