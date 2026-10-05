# Implantação do ProductHub com Azure App Service

Esta é a arquitetura adotada após a indisponibilidade de VMs na assinatura Azure for Students.

```text
Blob Static Website → App Service (API .NET 8) → Azure SQL Database
                         ├→ Application Insights / Log Analytics
                         └→ Managed Identity → Key Vault
```

## Por que App Service

App Service é um serviço gerenciado para aplicações web e APIs. Ele elimina administração de sistema operacional, SSH, patches e proxy reverso; também oferece URL HTTPS, integração com Managed Identity, Application Insights e deploy por GitHub Actions. Para uma única API .NET do MVP, é mais simples e seguro que Azure Container Instances. Container Apps é uma evolução viável quando houver necessidade de imagem Docker, escala para zero ou múltiplos microsserviços.

## Criar recursos no Portal

Use o Resource Group já existente e uma região permitida pela assinatura. O Azure SQL existente permanece em Canada Central; o App Service pode estar em outra região permitida, se necessário.

1. Crie ou reutilize `law-producthub` (Log Analytics Workspace).
2. Crie `appi-producthub` (Application Insights) conectado ao workspace; deixe OTLP como **Off**.
3. Crie `kv-producthub-<sufixo>` com permission model **Azure role-based access control**.
4. No Key Vault, crie os segredos `ConnectionStrings--SqlServer` e `ApplicationInsights--ConnectionString`.
5. Pesquise **App Services** → **Create** → **Web App** e selecione:
   - Publish: **Code**
   - Runtime stack: **.NET 8 (LTS)**
   - Operating System: **Linux**
   - Region: uma região permitida que aceite o plano
   - Pricing plan: o menor plano disponível compatível com a assinatura; examine a estimativa antes de criar.
6. Após criar: App Service → **Identity** → **System assigned** → `On` → Save.
7. No Key Vault → **Access control (IAM)**, atribua à identidade do App Service a função **Key Vault Secrets User**.
8. No App Service → **Environment variables** → **App settings**, crie:

```text
KeyVaultUri = https://NOME-DO-VAULT.vault.azure.net/
```

O código usa `DefaultAzureCredential`: no App Service, ela usa essa Managed Identity para ler os segredos do Key Vault.

## Primeiro deploy manual

No computador local:

```powershell
cd C:\Users\caius\Documents\Projects\Infnet\AzureCloud\src\ProductApi
dotnet publish -c Release -o publish
```

No Portal: App Service → **Deployment Center** → escolha método de publicação compatível, ou use o perfil de publicação no Visual Studio/VS Code. Depois abra:

```text
https://NOME-DO-APP.azurewebsites.net/swagger
```

Teste um `POST /api/products`. A tabela `Products` é criada automaticamente no Azure SQL na primeira inicialização.

## Frontend

Edite `frontend/config.js` antes de enviar ao Blob Static Website:

```js
window.APP_CONFIG = { apiBaseUrl: "https://NOME-DO-APP.azurewebsites.net" };
```

Envie os quatro arquivos da pasta `frontend/` ao container `$web` da Storage Account e abra o Primary endpoint. App Service já oferece HTTPS, evitando mixed content.

## Evidências

- Overview do App Service e URL HTTPS.
- Swagger retornando `200` e um `POST` bem-sucedido.
- System-assigned Identity como `On`.
- Role `Key Vault Secrets User` para a identidade do App Service.
- Application Insights com consultas `requests` e `dependencies`.
- Frontend hospedado no Blob consumindo a API.
