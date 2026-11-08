# Azure para OrderFlow — referencia práctica (`docs/azure.md`)

Complemento del ADR 14 y de la fase F12. Aquí van servicios, costos, comandos y paquetes; las **decisiones** viven en el
ADR. Fecha: 2026-11-06.

> Los niveles gratuitos y precios cambian. Antes de crear recursos, revisa la página de precios de cada servicio y deja
> configurada una **alerta de presupuesto** en la suscripción.

## 1. Servicios que usa OrderFlow y su costo aproximado para practicar

| Servicio | Para qué en OrderFlow | Costo para desarrollo (revisar vigente) |
| --- | --- | --- |
| Azure Container Apps | hospedar la API | concesión mensual gratuita de vCPU-segundos, memoria y requests; escala a cero |
| Azure SQL Database | catálogo, base compartida, dedicadas | oferta serverless gratuita con límite mensual de cómputo y almacenamiento |
| Azure Functions (Flex Consumption) | consumir eventos de Service Bus / Blob | concesión gratuita mensual de ejecuciones y GB-s; revisar cifras vigentes |
| Service Bus (Standard) | eventos del Outbox (topics) | costo base bajo por mes; Basic no tiene topics |
| Blob Storage (LRS) | exportaciones de tenant | 5 GB gratis los primeros 12 meses en cuentas nuevas |
| Key Vault (Standard) | secretos que no admiten Managed Identity | no es gratis: centavos por cada 10,000 operaciones |
| Application Insights | telemetría | unos 5 GB al mes de ingesta gratis vía Log Analytics |

Notas de vigencia:
- **Functions:** usa el **modelo isolated** (`Microsoft.Azure.Functions.Worker`); el in-process está en retiro.
  El plan **Linux Consumption** ya no recibe funciones nuevas y se retira en 2028: usa **Flex Consumption**.
  El soporte de **.NET 8 y 9 en Functions termina el 10 de noviembre de 2026**: crea todo en **.NET 10**.
- **Nombres únicos globales:** Storage, Key Vault, Function App y el servidor de Azure SQL deben ser únicos en todo
  Azure. Storage solo admite minúsculas y números (3–24).

## 2. Crear lo básico con Azure CLI (para aprender; lo definitivo va en Bicep)

```bash
az login
az group create -n rg-orderflow-dev -l eastus

# Storage (exportaciones y almacenamiento de la Function)
az storage account create -n storderflowdev01 -g rg-orderflow-dev --sku Standard_LRS --allow-shared-key-access false

# Key Vault con autorización por RBAC
az keyvault create -n kv-orderflow-dev01 -g rg-orderflow-dev --enable-rbac-authorization true

# Application Insights (basado en Log Analytics)
az monitor app-insights component create -a ai-orderflow-dev -g rg-orderflow-dev -l eastus

# Function App: isolated, .NET 10, Flex Consumption
az functionapp create -n func-orderflow-dev01 -g rg-orderflow-dev \
  --storage-account storderflowdev01 --flexconsumption-location eastus \
  --runtime dotnet-isolated --runtime-version 10.0 --app-insights ai-orderflow-dev

# Managed Identity para la Function y permisos mínimos
az functionapp identity assign -n func-orderflow-dev01 -g rg-orderflow-dev
```

Verifica los parámetros con `az functionapp create --help` antes de correrlo: las opciones de Flex Consumption han
cambiado entre versiones de la CLI. Al terminar de practicar: `az group delete -n rg-orderflow-dev` borra todo.

## 2.1 Azure SQL para OrderFlow (F1, ADR 15)

En F1 esto se define en `infra/main.bicep`; los comandos sirven para entender qué hace cada pieza.

```bash
# Tu id de objeto en Entra (para ser el administrador del servidor)
az ad signed-in-user show --query "{id:id, upn:userPrincipalName}" -o table

# Servidor lógico con autenticación SOLO Entra ID (sin logins SQL)
az sql server create -n sql-orderflow-dev01 -g rg-orderflow-dev -l eastus \
  --enable-ad-only-auth --external-admin-principal-type User \
  --external-admin-name <tu UPN> --external-admin-sid <tu id de objeto>

# Firewall: tu IP pública actual
az sql server firewall-rule create -s sql-orderflow-dev01 -g rg-orderflow-dev -n casa \
  --start-ip-address <tu IP> --end-ip-address <tu IP>

# Bases free tier con pausa automática al agotar la cuota (verifica los parámetros con --help)
az sql db create -s sql-orderflow-dev01 -g rg-orderflow-dev -n OrderFlowCF_Catalog \
  --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 2 \
  --use-free-limit --free-limit-exhaustion-behavior AutoPause
az sql db create -s sql-orderflow-dev01 -g rg-orderflow-dev -n OrderFlowCF_Shared \
  --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 2 \
  --use-free-limit --free-limit-exhaustion-behavior AutoPause

# Comprobar que de verdad son free tier
az sql db show -s sql-orderflow-dev01 -g rg-orderflow-dev -n OrderFlowCF_Catalog --query "{free:useFreeLimit, onExhaust:freeLimitExhaustionBehavior}"
```

Cadena de conexión de desarrollo (en `user-secrets`, aunque no lleva secretos):
```
Server=tcp:sql-orderflow-dev01.database.windows.net,1433;Database=OrderFlowCF_Catalog;Authentication=Active Directory Default;Encrypt=True;Connect Timeout=60
```

Tenant dedicado (F4), creado por el servicio de alta **antes** de `MigrateAsync`, nunca por EF:
```sql
CREATE DATABASE [OrderFlowCF_Hooli] (EDITION = 'Basic', SERVICE_OBJECTIVE = 'Basic', MAXSIZE = 2 GB);
```

MCP de Azure para Claude Code (una línea en PowerShell):
```powershell
claude mcp add sqlserver-azure "C:\Users\Alejandro Torres\source\repos\SqlServerMcp\publish\SqlServerMcp.exe" --transport stdio --scope user --env "MSSQL_CONNECTION_STRING=Server=tcp:sql-orderflow-dev01.database.windows.net,1433;Database=OrderFlowCF_Shared;Authentication=Active Directory Default;Encrypt=True;Connect Timeout=60" --env "MSSQL_ALLOWED_DATABASES=OrderFlowCF*" --env "MSSQL_TENANT_DATABASES=OrderFlowCF_*" --env MSSQL_TENANT_TEMPLATE=OrderFlowCF_Shared
```
Nota: el patrón `OrderFlowCF_*` también incluye al catálogo; en `compare_schemas` ignóralo (no es un tenant).
En Azure SQL Database cada conexión ve su base y `master`; las herramientas del MCP que consultan
`sys.databases` muestran menos información que en Docker.

Hábitos de costo: cerrar SSMS/Azure Data Studio al terminar (una conexión abierta impide la pausa), alerta en la
métrica *Free amount remaining* (avisar al bajar de 10,000 vCore-segundos), y dar de baja los tenants dedicados de
prueba.

## 3. Paquetes NuGet clave

| Paquete | Uso |
| --- | --- |
| `Azure.Identity` | `DefaultAzureCredential`: tu sesión de VS / `az login` en local, Managed Identity en Azure |
| `Azure.Extensions.AspNetCore.Configuration.Secrets` + `Azure.Security.KeyVault.Secrets` | Key Vault como `IConfiguration` |
| `Azure.Storage.Blobs` | exportaciones de tenant |
| `Azure.Messaging.ServiceBus` | publicar eventos desde el despachador del Outbox |
| `Microsoft.Azure.Functions.Worker` (+ extensiones de Service Bus / Blob) | Functions en modelo isolated |
| `Azure.Monitor.OpenTelemetry.AspNetCore` | exportar a App Insights la telemetría OpenTelemetry que ya existe desde F0 |
| `Microsoft.Data.SqlClient` | autenticación a Azure SQL con Managed Identity (`Authentication=Active Directory Default`) |

## 4. Conceptos de entrevista y del día a día

- **Managed Identity + RBAC** en lugar de connection strings: la Function recibe roles como `Key Vault Secrets User`,
  `Storage Blob Data Contributor` o `Azure Service Bus Data Receiver`; nada de llaves en configuración.
- **Triggers y bindings de Functions:** HTTP, Timer, Blob, Queue, Service Bus; cuándo un binding y cuándo el SDK.
- **Durable Functions:** orquestaciones con estado (p. ej. el alta de un tenant dedicado como orquestación con pasos y
  compensación).
- **Flex Consumption vs Premium vs Dedicated:** arranque en frío, escalado, red privada, costo.
- **Container Apps vs App Service vs AKS:** cuándo elegir cada uno.
- **Azure SQL:** serverless vs provisionado, elastic pools para muchos tenants, autenticación con Entra ID.
- **Service Bus vs Storage Queues vs Event Grid:** topics y suscripciones, sesiones, dead-letter, reintentos.
- **Infraestructura como código:** Bicep (nativo) vs Terraform (multinube).
- **CI/CD sin secretos:** GitHub Actions con OIDC federado contra Entra ID.

## 5. Otros servicios que vale la pena conocer (fuera del núcleo)

- **API Management:** el equivalente a lo que haces en MuleSoft (políticas, cuotas, versiones, portal de desarrolladores).
- **App Service:** tiene plan F1 gratuito; alternativa más simple a Container Apps.
- **Cosmos DB:** capa gratuita; útil para conocerlo, descartado para OrderFlow (modelo relacional, RLS de SQL Server).
- **Entra ID / Entra External ID:** autenticación de usuarios (opción del ADR 13).
- **Azure DevOps:** alternativa a GitHub Actions, común en empresas.
EOF
cd /mnt/user-data/outputs/orderflow-arquitectura && grep -n "docs/azure.md\|ADR 14" ARQUITECTURA.md | head