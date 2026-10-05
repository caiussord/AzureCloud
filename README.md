# ProductHub Azure

MVP de cadastro de produtos para a disciplina Azure Cloud.

## Arquitetura

`Frontend estático (Blob Storage) -> API .NET 8 (Azure App Service) -> Azure SQL Database`

O frontend chama a API pelo endereço configurado em `frontend/config.js`. Em desenvolvimento, a API usa SQLite local; em produção, configure `ConnectionStrings__SqlServer` para usar Azure SQL.

