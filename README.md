# ProductHub Azure

MVP de cadastro de produtos para a disciplina Azure Cloud.

## Arquitetura

`Frontend estático (Blob Storage) -> API .NET 8 (VM) -> Azure SQL Database`

O frontend chama a API pelo endereço configurado em `frontend/config.js`. Em desenvolvimento, a API usa SQLite local; em produção, configure `ConnectionStrings__SqlServer` para usar Azure SQL.

## Executar localmente

1. Abra um terminal em `src/ProductApi` e execute `dotnet run`.
2. Copie a URL HTTP exibida, por exemplo `http://localhost:5183`.
3. Em `frontend/config.js`, altere `apiBaseUrl` para essa URL.
4. Abra `frontend/index.html` no navegador ou sirva a pasta com qualquer servidor estático.

Com a API em execução, o Swagger está disponível em `/swagger` (por exemplo, `http://localhost:5183/swagger`).

## Publicação na Azure (visão geral)

1. Crie Resource Group, Storage Account com Static Website, VM Linux, Azure SQL e Log Analytics.
2. Na VM, publique a API com `dotnet publish`, configure o serviço systemd e um proxy Nginx/HTTPS.
3. Na configuração da API, defina `ConnectionStrings__SqlServer` (não a coloque no Git).
4. Faça upload dos arquivos de `frontend/` no container `$web` e configure `apiBaseUrl` com o domínio HTTPS da API.

Leia [o guia de implantação](docs/implantacao.md) antes de criar recursos pagos.
