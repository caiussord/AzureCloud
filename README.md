# ProductHub Azure

MVP de cadastro de produtos para a disciplina Azure Cloud.

## Arquitetura

`Frontend estático (Blob Storage) -> API .NET 8 (Azure App Service) -> Azure SQL Database`

O frontend chama a API pelo endereço configurado em `frontend/config.js`. Em desenvolvimento, a API usa SQLite local; em produção, configure `ConnectionStrings__SqlServer` para usar Azure SQL.

## Executar localmente

1. Abra um terminal em `src/ProductApi` e execute `dotnet run`.
2. Copie a URL HTTP exibida, por exemplo `http://localhost:5183`.
3. Em `frontend/config.js`, altere `apiBaseUrl` para essa URL.
4. Abra `frontend/index.html` no navegador ou sirva a pasta com qualquer servidor estático.

Com a API em execução, o Swagger está disponível em `/swagger` (por exemplo, `http://localhost:5183/swagger`).

## Publicação na Azure (visão geral)

1. Crie Resource Group, Storage Account com Static Website, App Service Linux, Azure SQL e Application Insights/Log Analytics.
2. Publique a API .NET diretamente no App Service. A plataforma já fornece HTTPS e não requer VM, Nginx ou SSH.
3. Habilite a identidade gerenciada do App Service e configure `KeyVaultUri` (não coloque a connection string no Git).
4. Faça upload dos arquivos de `frontend/` no container `$web` e configure `apiBaseUrl` com o domínio HTTPS da API.

Leia o [guia de App Service](docs/implantacao-app-service.md) antes de criar recursos pagos.
