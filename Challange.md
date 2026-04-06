# Desafio Técnico — Arquiteto de Soluções

## Papel do Arquiteto de Soluções

O Arquiteto de Soluções é responsável por compreender e transformar requisitos de negócios, sejam funcionais ou não-funcionais, em capacidades e competências que permitam a realização de atividades que gerem valor para a organização.

É responsabilidade do Arquiteto de Soluções:
- Desenhar arquiteturas de contexto com distribuição clara de responsabilidades entre processos, sistemas e domínios.
- Definir conceitos e estruturar soluções alinhadas à cadeia de valor do negócio.
- Projetar soluções escaláveis, reutilizáveis e resilientes.
- Garantir segurança, observabilidade e governança da solução.
- Comunicar decisões arquiteturais de forma clara, estruturada e fundamentada.

Além disso, espera-se que o arquiteto demonstre capacidade de justificar decisões técnicas e arquiteturais com base em boas práticas, trade-offs e princípios de arquitetura.
 
# Objetivo do Desafio

Desenvolver uma arquitetura que integre processos e sistemas de forma eficiente, garantindo entrega de valor para a organização.

## A solução proposta deve demonstrar:

- Capacidade analítica
- Visão sistêmica
- Tomada de decisão arquitetural
- Comunicação técnica estruturada
- Preocupação com segurança e operação da solução

## A solução deve abordar:

- Contextos de negócio
- Capacidades e domínios funcionais
- Padrões arquiteturais
- Requisitos não funcionais
- Segurança
- Escalabilidade
- Operação e monitoramento
 
# Importante — Comunicação Arquitetural

Um dos principais objetivos deste desafio é avaliar a capacidade de comunicar decisões arquiteturais.

Portanto, a documentação deve obrigatoriamente incluir diagramas estruturados, preferencialmente seguindo o C4 Model, contemplando:

- Context Diagram
- Container Diagram
- Component Diagram (quando aplicável)
- Fluxos de interação entre serviços

Os diagramas devem deixar claras:

- responsabilidades de cada componente
- fluxo de dados
- dependências entre sistemas
- pontos de integração

Diagramas que não comunicarem claramente a arquitetura poderão ser considerados incompletos.
 
# Segurança (Obrigatório)

A solução deve obrigatoriamente apresentar um desenho de segurança, contemplando no mínimo:

- Autenticação
- Autorização
- Proteção de APIs
- Proteção de dados sensíveis
- Estratégia de criptografia
- Controle de acesso entre serviços
- Estratégias contra ataques comuns (ex: rate limit, validação, etc.)

Além disso, deve existir documentação justificando as decisões de segurança adotadas.

A ausência de definição de segurança será considerada falha crítica na solução.
 
# Fundamentação das Decisões

Toda decisão arquitetural relevante deve possuir justificativa técnica clara, incluindo:

- trade-offs considerados
- alternativas descartadas
- motivos da escolha da arquitetura
- justificativa das tecnologias utilizadas

Sempre que possível, utilize referências a boas práticas ou padrões conhecidos de arquitetura.
 
# Priorização de Requisitos

Espera-se que o candidato demonstre capacidade de priorização arquitetural, explicitando:

- o que foi priorizado na solução
- o que ficou fora do escopo inicial
- riscos ou limitações da solução proposta

Essa priorização deve ser documentada.
 
# Descritivo da Solução

Um comerciante precisa controlar o seu fluxo de caixa diário com os lançamentos (débitos e créditos).

Além disso, precisa de um relatório que disponibilize o saldo diário consolidado.
 
# Requisitos de Negócio

- Serviço responsável pelo controle de lançamentos
- Serviço responsável pelo consolidado diário
 
# Requisitos Obrigatórios

A solução deve obrigatoriamente conter:

## 1 — Arquitetura e Domínios

- Mapeamento de domínios funcionais
- Identificação de capacidades de negócio
- Definição de limites de responsabilidade

## 2 — Levantamento de Requisitos

- Refinamento de requisitos funcionais
- Definição de requisitos não funcionais

## 3 — Arquitetura da Solução (Arquitetura Alvo)

- Diagramas arquiteturais claros
- Componentes e responsabilidades
- Fluxos de comunicação
- Padrões arquiteturais adotados

## 4 — Segurança

- Estratégia de autenticação
- Estratégia de autorização
- Proteção de dados
- Segurança de APIs
- Estratégia de comunicação segura entre serviços

## 5 — Implementação

- Implementação funcional mínima
- Testes automatizados
- Código versionado em repositório público

## 6 — Operação da Solução

A solução deve incluir definição de operação, contendo:

- estratégia de deploy
- monitoramento
- logs
- observabilidade
- escalabilidade
- recuperação de falhas

## 7 — Documentação

A documentação deve seguir uma estrutura organizada no repositório contendo ao menos:

/docs
  /architecture
  /security
  /decisions
  /operations

Também devem existir registros de decisões arquiteturais (ADR).
 
# Requisitos Diferenciais

Serão considerados diferenciais:

- Arquitetura de Transição (caso exista migração de legado)
- Estimativa de custos de infraestrutura
- Estratégia de monitoramento e observabilidade
- Critérios de segurança para integração entre serviços
- Uso estruturado de ADR (Architecture Decision Records)
 
# Requisitos Não Funcionais

- O serviço de controle de lançamentos não deve ficar indisponível caso o serviço de consolidado diário falhe.
- Em picos, o serviço de consolidado recebe 50 requisições por segundo, com no máximo 5% de perda de requisições.

A arquitetura deve demonstrar como esses requisitos são atendidos.
 
# Entregáveis

O candidato deverá entregar:

- Repositório público (GitHub)
- Código da solução
- Testes
- Documentação arquitetural
- Diagramas estruturados
- README com instruções para execução
 
# Observação Final

O objetivo deste desafio não é apenas avaliar implementação, mas principalmente:

- maturidade arquitetural
- capacidade de comunicação técnica
- visão de segurança
- fundamentação de decisões
- visão de operação da solução