# Roteiro de entrega e evidências

Este documento é o checklist da apresentação. Cada print deve mostrar a barra do Portal Azure com a conta/subscrição quando possível, o nome do recurso e a data/hora do computador. Nunca capture senha, connection string, chave privada SSH ou valor de segredo.

## Estado atual do repositório

| Item | Estado | Evidência local |
|---|---|---|
| Frontend estático | Pronto | `frontend/index.html` e tela do navegador com lista de produtos |
| API REST .NET 8 | Pronto | `src/ProductApi`, Swagger e endpoint `/health` |
| CRUD e modelo relacional | Pronto | Swagger: GET/POST/PUT/DELETE `/api/products` |
| Azure SQL | Preparado, não provisionado | API usa `ConnectionStrings:SqlServer` quando configurada |
| VM, Blob Static Website, Monitor | Não provisionados | Executar as etapas deste roteiro no Portal |
| Application Insights | Código pronto; falta recurso/configuração | Connection string configurada como segredo |
| Key Vault + Managed Identity | Código pronto; falta recurso/configuração | `KeyVaultUri` e segredo `ConnectionStrings--SqlServer` |
| Azure Policy / Defender / Landing Zone | Falta configurar e registrar | Etapas 7 e 8 |
| IaC e CI/CD | Ainda falta adicionar ao repositório | Próxima etapa de implementação |

## 1. Base do MVP — faça primeiro

1. No Portal, crie o Resource Group `rg-producthub-dev`; aplique as tags `Projeto=ProductHub`, `Ambiente=Dev` e `Aluno=<seu nome>`.
2. Crie Azure SQL Database, uma VM Linux e uma Storage Account com Static Website, todos no mesmo Resource Group e região.
3. Publique a API na VM e o frontend no container `$web`, conforme `docs/implantacao.md`.
4. Em `frontend/config.js`, troque a URL local pelo endereço público HTTPS da API antes de fazer upload.

**Prints obrigatórios:**

- E01: Overview do Resource Group mostrando Storage Account, VM, SQL Database, Key Vault, Application Insights e Log Analytics (este print é feito no final).
- E02: Swagger no navegador, com `GET /api/products` expandido e uma resposta `200`.
- E03: Endpoint público do Static Website com pelo menos um produto salvo.
- E04: Overview do Azure SQL Database, sem expor a connection string.
- E05: Overview da VM, mostrando status `Running`, imagem Ubuntu e IP ocultado parcialmente se desejar.

## 2. Azure SQL: edições e decisão

No relatório, declare que foi escolhido **Azure SQL Database single database**, pois o MVP é uma única aplicação, possui baixo volume e modelo relacional. Compare:

| Opção | Use quando | Trade-off |
|---|---|---|
| Single database | Um banco isolado por aplicação | Administração simples; menos ganho de custo se houver muitos bancos pequenos |
| Elastic pool | Vários bancos com uso oscilante | Compartilha recursos; exige planejar e monitorar o pool |
| Serverless | Carga intermitente de desenvolvimento | Pode reduzir custo ocioso; tem atraso ao retomar e não é ideal para carga sempre ativa |
| Provisioned vCore | Carga previsível/produção | Desempenho reservado; paga mesmo quando pouco usado |
| Hyperscale | Banco muito grande e crescimento intenso | Escala alta; maior complexidade/custo, excessivo para o MVP |
| Managed Instance | Maior compatibilidade com SQL Server legado | Mais recursos e custo; não é necessário aqui |

**Prints:**

- E06: Tela `Configure database` antes de criar, com o tier escolhido visível (remova/oculte qualquer preço se a instituição pedir).
- E07: Query Editor do Azure SQL mostrando `SELECT * FROM Products` após cadastrar pelo frontend.

## 3. Cosmos DB: avaliação, não é necessário provisionar

Não crie Cosmos DB somente para este MVP; documente a decisão. Uma modelagem hipotética é um container `products` com documentos de produto e chave de partição `/tenantId` num cenário multiempresa. `/category` só seria adequada se as consultas e a distribuição fossem equilibradas; categorias populares podem gerar hot partition.

Explique que RUs medem capacidade/consumo de operações; leituras/escritas, índice e consulta entre partições alteram o consumo. Use consistência **Session** como decisão recomendada para leitura consistente por usuário com bom equilíbrio de custo. Strong aumentaria a garantia e o impacto em disponibilidade/latência; Eventual reduz custo/latência, mas pode retornar leitura desatualizada.

**Evidência:** E08: print da seção correspondente deste documento no seu relatório/PDF, contendo modelo, chave `/tenantId`, RUs e consistência Session. Não invente um recurso Azure que não foi criado.

## 4. Azure SQL versus Cosmos DB e migração (6 R's)

Registre: SQL foi escolhido porque os dados de produto têm esquema estável, integridade e transações. Cosmos seria escolhido para escala global, baixa latência multi-região e esquema altamente variável.

Para uma migração de catálogo SQL pequeno, escolha **Replatform**: mover para Azure SQL com pequena adaptação da connection string e testes. Compare também Rehost, Refactor, Repurchase, Retire e Retain; justifique que LGPD, downtime, orçamento, compatibilidade e rollback condicionam a decisão.

**Evidência:** E09: print desta matriz incluída no relatório/PDF, junto de uma captura da tabela `Products` no Query Editor (E07).

## 5. Telemetria: Monitor, Log Analytics e Application Insights

1. Crie `law-producthub` (Log Analytics Workspace).
2. Na VM, abra **Insights** → **Enable** e selecione esse workspace. Isso instala Azure Monitor Agent e cria/associa uma Data Collection Rule.
3. Crie Application Insights `appi-producthub`, conectado ao mesmo workspace.
4. Copie somente a connection string para o Key Vault, no segredo `ApplicationInsights--ConnectionString`.
5. Com Key Vault configurado, a API enviará Request, Dependency, Exception e traces automaticamente.
6. Gere tráfego: abra Swagger, execute GET e POST, e abra o frontend.
7. Em Application Insights → Logs, consulte:

```kusto
requests
| where timestamp > ago(30m)
| project timestamp, name, resultCode, duration, success
| order by timestamp desc
```

No Log Analytics, valide a VM:

```kusto
Heartbeat
| where TimeGenerated > ago(30m)
| project Computer, TimeGenerated, OSType
| order by TimeGenerated desc
```

**Prints:** E10: VM Insights habilitado; E11: resultado da query `Heartbeat`; E12: resultado da query `requests`; E13: regra de alerta de CPU ou indisponibilidade criada.

## 6. Key Vault, identidade gerenciada e LGPD

1. Crie `kv-producthub-<sufixo-unico>` com **Azure role-based access control** habilitado.
2. Crie os segredos `ConnectionStrings--SqlServer` e `ApplicationInsights--ConnectionString`. O valor é secreto e não deve aparecer em print.
3. Na VM → **Identity** → **System assigned**, marque `On` e salve.
4. No Key Vault → **Access control (IAM)** → **Add role assignment**, conceda à identidade da VM a função **Key Vault Secrets User**.
5. Na configuração da API, mantenha apenas `KeyVaultUri=https://NOME-DO-VAULT.vault.azure.net/`; não coloque senha no repositório nem no serviço systemd.
6. Reinicie `productapi` e teste Swagger. Se a API abrir e gravar no Azure SQL, a identidade está recuperando o segredo.

**Prints:** E14: Identity da VM como `On`; E15: role assignment da VM no Key Vault; E16: lista de segredos mostrando apenas os nomes, nunca valores; E17: Swagger funcionando depois da troca.

## 7. Política e Landing Zone

Na assinatura ou no Resource Group (para evitar bloquear outros recursos), abra **Policy** → **Assignments** → **Assign policy** e atribua a política built-in `Require a tag and its value on resources` para exigir a tag `Projeto=ProductHub`. Execute a avaliação de compliance e corrija recursos sem tag.

Descreva a Landing Zone acadêmica: uma assinatura, um Resource Group por ambiente (`rg-producthub-dev` agora; `rg-producthub-prod` futuro), RBAC de menor privilégio, tags, Policy, Log Analytics e Defender centralizados. Isso é uma versão proporcional do conceito de Landing Zone; não tente criar uma estrutura corporativa de management groups sem permissão.

Azure Blueprints está em aposentadoria faseada: desde 31/07/2026 novas definições/versões não podem ser criadas e a aposentadoria total está prevista para 31/01/2027. Para a entrega, justifique usar **Azure Policy + Bicep/Template Specs** como substitutos atuais.

**Prints:** E18: Policy assignment; E19: Compliance mostrando inicialmente o estado e, após corrigir tags, estado compliant; E20: diagrama da Landing Zone acadêmica no relatório.

## 8. Defender for Cloud

Abra **Microsoft Defender for Cloud** → **Recommendations**. Revise as recomendações da VM, Storage e SQL. Corrija apenas o que cabe ao MVP, por exemplo: regras NSG restritas, identidade gerenciada ligada, HTTPS planejado e alertas habilitados. Se uma recomendação exigir SKU pago, registre-a como "não implementada por restrição de créditos", com justificativa e plano de evolução.

**Prints:** E21: Recommendations filtradas para o Resource Group; E22: uma recomendação corrigida ou a justificativa documentada no relatório.

## 9. IaC, CI/CD e Well-Architected

Estes artefatos serão adicionados ao repositório antes da entrega: Bicep para recursos e GitHub Actions para build/teste/deploy. O deploy produtivo da API permanece na VM, conforme requisito do MVP. Como a rubrica menciona App Service/Functions, o relatório terá um pipeline de evolução para uma Function/App Service, sem substituir a evidência da VM.

**Prints futuros:** E23: arquivo Bicep no GitHub e execução/validação; E24: execução verde do GitHub Actions; E25: diagrama Well-Architected e tabela de trade-offs/priorização.
