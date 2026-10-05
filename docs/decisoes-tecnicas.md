# Decisões técnicas e defesa da arquitetura

## Escolha do banco

O MVP usa **Azure SQL Database**. Produtos possuem campos previsíveis, consultas relacionais simples, regras de integridade e baixo volume. O modelo relacional reduz complexidade e permite usar SQL conhecido pela equipe.

Azure SQL Database possui opções DTU e vCore, com modalidades provisionada e serverless. Para estudo e baixa utilização, a opção mais econômica disponível deve ser escolhida; em produção, a decisão depende de previsibilidade de carga, desempenho e necessidade de recursos como alta disponibilidade. Azure SQL Managed Instance é indicada quando há maior compatibilidade com SQL Server tradicional; SQL Database é mais simples para este SaaS pequeno.

## Quando Cosmos DB seria apropriado

Cosmos DB seria mais adequado para catálogo global, alta escala, dados com esquema variável ou baixa latência em múltiplas regiões. Uma possível modelagem seria um container `products`, com documento contendo produto e metadados, e chave de partição `/category` se as consultas forem majoritariamente por categoria. Se uma categoria concentrar muitos produtos, a distribuição ficaria desequilibrada; nesse caso, usar uma chave de maior cardinalidade, como `/tenantId`, é melhor em um cenário multiempresa.

As RUs são capacidade consumida por leituras, gravações e consultas; índices e consultas entre partições elevam consumo. Para o catálogo, consistência **Session** equilibra experiência consistente por usuário e custo/desempenho. Strong só seria escolhida se a regra de negócio exigisse leitura imediatamente consistente globalmente, com o custo e as limitações correspondentes.

## Migração e 6 R's

Para uma base SQL existente pequena, a estratégia provável é **Rehost** inicialmente (mover com Azure Database Migration Service/backup compatível) e evoluir para **Replatform** no Azure SQL Database. As demais possibilidades são: Rehost, Replatform, Refactor, Repurchase, Retire e Retain. A escolha deve considerar downtime permitido, LGPD, dependências, orçamento e compatibilidade. Antes de migrar: classificar dados pessoais, testar em ambiente separado, validar contagens/checksums e preparar rollback.

## Segurança, LGPD e governança

Dados pessoais não são necessários para o MVP. Caso usuários sejam incluídos, colete o mínimo necessário, documente finalidade e retenção, proteja dados em trânsito (HTTPS) e em repouso, e evite dados pessoais nos logs. Segredos ficam no Key Vault; o App Service usa Managed Identity em vez de senha no código. Aplicar RBAC de menor privilégio, tags de custo, Azure Policy para exigir tags e impedir recursos públicos indevidos. Defender for Cloud deve ser revisado periodicamente e recomendações críticas devem ser tratadas ou justificadas.

## Well-Architected e evolução

| Pilar | Decisão atual | Próxima evolução |
|---|---|---|
| Confiabilidade | App Service gerenciado, adequado ao MVP | Slot de implantação, backup e escala horizontal |
| Segurança | Key Vault, identidade gerenciada, NSG restritivo | WAF, autenticação e análise contínua do Defender |
| Otimização de custos | Plano App Service mínimo e orçamento | Otimizar plano, escala e monitorar consumo |
| Excelência operacional | Logs centralizados e documentação | CI/CD, runbooks e alertas acionáveis |
| Eficiência de desempenho | API REST e SQL indexado por chave | Cache e escala conforme métricas |

Prioridades: (1) implantar o MVP no App Service com segurança; (2) telemetria e orçamento; (3) pipeline CI/CD e IaC; (4) autenticação; (5) avaliar Azure Container Apps se houver necessidade de imagem Docker, escala para zero ou múltiplos microsserviços.
