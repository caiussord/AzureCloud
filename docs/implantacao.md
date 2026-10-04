# Guia de implantação econômica

> Atenção: crie recursos somente na região e SKU disponíveis em sua assinatura. Antes de confirmar cada tela do Portal Azure, confira a estimativa mostrada e acompanhe **Cost Management + Billing**.

## 1. Preparação

Crie um Resource Group, por exemplo `rg-producthub-dev`, e aplique as tags `Projeto=ProductHub`, `Ambiente=Estudo` e `Responsavel=<seu-nome>`. Defina um orçamento baixo com alerta em Cost Management.

## 2. Banco de dados

Crie um Azure SQL Database com a menor opção disponível para a assinatura acadêmica. Crie também o Logical SQL Server. No firewall, inicialmente permita somente o IP público da VM (e, temporariamente para testes, seu IP). Guarde a connection string: ela contém segredo e nunca deve ser enviada ao Git.

Na VM, defina a variável de ambiente `ConnectionStrings__SqlServer` com uma string como:

```text
Server=tcp:SEU-SERVIDOR.database.windows.net,1433;Initial Catalog=producthub;User ID=USUARIO;Password=SENHA;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

A API cria a tabela `Products` na primeira inicialização.

## 3. API na VM

Crie uma VM Linux pequena, autentique com chave SSH e deixe abertas somente as portas necessárias: `22` limitada ao seu IP e `80/443` ao público. Instale o runtime ASP.NET 8, envie o resultado de `dotnet publish -c Release -o publish` e crie um serviço `systemd` para executar `ProductApi.dll`.

Use Nginx como proxy reverso e HTTPS com certificado válido. Não exponha a porta interna Kestrel (por exemplo, 5000) diretamente à internet.

## 4. Frontend no Blob Storage

Crie uma Storage Account e habilite **Static website**. Faça upload de `frontend/index.html`, `styles.css`, `app.js` e `config.js` para o container `$web`. Antes disso, edite `config.js` com a URL HTTPS pública da API. Mantenha o acesso de dados de blobs privado; a exceção é o endpoint de site estático, que serve apenas arquivos públicos do frontend.

## 5. Monitoramento

Crie um Log Analytics Workspace. Em **Azure Monitor**, configure diagnóstico da VM para enviar logs ao workspace e crie um alerta simples para CPU alta ou indisponibilidade. Para a API, Application Insights pode ser adicionado numa evolução; registre ao menos os logs do serviço com `journalctl` e os eventos da VM no workspace.

## Segurança e encerramento

- Use Key Vault para armazenar a string de conexão e dê à identidade gerenciada da VM permissão de leitura do segredo.
- Não deixe regras de firewall com `0.0.0.0/0` no SQL nem SSH aberto para toda a internet.
- Ao terminar a demonstração, **pare/desaloque a VM** e revise os recursos. Não exclua o Resource Group se ainda precisar das evidências para a entrega.
