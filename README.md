# 🏢 Marketplace Inmobiliario - Backend API (.NET 10)

API RESTful Serverless/PaaS desarrollada en **.NET 10** y **C# 14** para el Marketplace Inmobiliario White-Label. Diseñada bajo principios de Clean Architecture, alta disponibilidad, seguridad JWT, rate limiting y almacenamiento seguro de activos en la nube con Cloudinary.

---

## 🛠️ Tecnologías y Librerías Utilizadas

- **Runtime & Lenguaje:** .NET 10 (ASP.NET Core Web API) & C# 14
- **Base de Datos & ORM:** PostgreSQL en **Neon.tech** con Entity Framework Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Autenticación & Seguridad:** JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), BCrypt.Net-Next, Fixed-Window Rate Limiting (`System.Threading.RateLimiting`)
- **Gestión de Imágenes:** Cloudinary SDK (`CloudinaryDotNet` v1.29.2)
- **Rendimiento:** Response Compression (Brotli & Gzip), MemoryCache, Health Checks
- **Contenerización:** Docker & Docker Compose

---

## 🏛️ Arquitectura Implementada

- **Patrón Controlador-Servicio (Controller-Service Pattern):** Separación estricta entre la capa HTTP (`Controllers`), lógica de negocio (`Services`), datos (`Data/EF Core`) y DTOs.
- **Seeder de Datos Automatizado (`DatabaseSeeder.cs`):** Inserción idempotente de usuario Administrador al iniciar la aplicación leyendo credenciales seguras.
- **EF Core Optimizations:** `ValueComparer<List<string>>` para columnas JSONB de PostgreSQL y filtro global de Soft Delete.
- **Patrón Options:** Inyección fuertemente tipada de configuraciones (`CloudinarySettings`, `JwtSettings`, `GeocodingSettings`).
- **Seguridad en Repositorio Público:** Base `appsettings.json` limpia con valores genéricos placeholders, sin secretos expuestos.

---

## 💻 Desarrollo Local

### Requisitos Previos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Instancia local de PostgreSQL o Docker Desktop.

### Configuración del Entorno de Desarrollo
1. Copia o crea el archivo `appsettings.Development.json` en la raíz del proyecto (este archivo se encuentra ignorado por `.gitignore`):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=marketplace_paysandu_dev;Username=postgres;Password=postgres"
     },
     "JwtSettings": {
       "SecretKey": "CLAVE_SECRETA_SOLO_PARA_DESARROLLO_LOCAL_NO_USAR_EN_PRODUCCION_PAYSANDU_2026!",
       "Issuer": "MarketplacePaysanduAPI_Dev",
       "Audience": "MarketplaceAdminPanel_Dev"
     },
     "CloudinarySettings": {
       "CloudName": "tu_cloud_name_local",
       "ApiKey": "tu_api_key_local",
       "ApiSecret": "tu_api_secret_local"
     }
   }
   ```
2. Ejecuta las migraciones de EF Core:
   ```bash
   dotnet ef database update
   ```
3. Inicia la API localmente (Puerto 5000):
   ```bash
   dotnet run --launch-profile http
   ```

---

## 🚀 Despliegue Oficial en Producción (Northflank + Neon.tech)

El proyecto está preparado para ejecutarse en infraestructura PaaS serverless de alto rendimiento.

### 1. Base de Datos en Neon.tech (PostgreSQL Serverless)
1. Crea un proyecto en [Neon.tech](https://neon.tech).
2. Obtén la cadena de conexión en formato PostgreSQL con soporte SSL obligatorio:
   ```
   Host=ep-cool-name-123456.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=vuestra_password_neon;SSL Mode=Require;Trust Server Certificate=true;
   ```

### 2. Despliegue del Servicio Web API en Northflank
1. Crea un nuevo **Service** (Combined Service / Web Service) en el panel de [Northflank](https://northflank.com).
2. Conecta este repositorio de GitHub en la rama `main`.
3. Selecciona la opción de **Buildpack** o **Dockerfile** (ubicado en `backend/Dockerfile`).
4. Asigna el puerto expuesto del contenedor: **`5000`** (o `8080`).

### 🔑 Variables de Entorno en Northflank
Para sobrescribir la configuración pública de GitHub sin alterar el código ni exponer credenciales, añade las siguientes **Environment Variables** en el panel de Northflank usando la notación `__` (doble guion bajo) propia de ASP.NET Core:

| Variable de Entorno | Valor de Ejemplo / Descripción |
| :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Host=ep-xxx.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;` |
| `JwtSettings__SecretKey` | `<CLAVE_SECRETA_JWT_MINIMO_32_CARACTERES_PRODUCCION>` |
| `JwtSettings__Issuer` | `MarketplacePaysanduAPI` |
| `JwtSettings__Audience` | `MarketplaceAdminPanel` |
| `AdminSeedSettings__Email` | `admin@alquilerespaysandu.com` |
| `AdminSeedSettings__Password` | `<PASSWORD_SEGURO_ADMIN_PRODUCCION>` |
| `CloudinarySettings__CloudName` | `<TU_CLOUDINARY_CLOUD_NAME>` |
| `CloudinarySettings__ApiKey` | `<TU_CLOUDINARY_API_KEY>` |
| `CloudinarySettings__ApiSecret` | `<TU_CLOUDINARY_API_SECRET>` |

---

## 🏥 Health Checks & Endpoints de Diagnóstico

- `GET /health`: Diagnóstico de salud de la aplicación y conexión a la base de datos PostgreSQL.
- `POST /api/upload`: Endpoint protegido con `[Authorize]` para subida directa de imágenes a Cloudinary.
