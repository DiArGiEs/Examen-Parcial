# 🏦 Plataforma de Solicitudes de Crédito — Examen Parcial

Aplicación web desarrollada en **ASP.NET Core MVC (.NET 8)** para la gestión y evaluación de solicitudes de créditos bancarios. Incluye autenticación con ASP.NET Core Identity, control de acceso basado en roles (`Analista`), cacheo distribuido con Redis y almacenamiento de estado de sesión.

---

## Tecnologías Utilizadas

- **Framework:** .NET 8 (ASP.NET Core MVC)
- **Persistencia:** Entity Framework Core (SQLite)
- **Autenticación & Autorización:** ASP.NET Core Identity
- **Cache & Sesión:** Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`) / Distributed Memory Cache Fallback
- **Contenedores & Despliegue:** Docker, Render.com

---

## Usuarios y Credenciales de Prueba (Seeding)

El sistema genera automáticamente los siguientes usuarios al iniciar la aplicación:

| Rol | Email | Contraseña |
| :--- | :--- | :--- |
| **Analista de Riesgo** | `analista@banco.com` | `Analista123!` |
| **Cliente 1** | `cliente1@banco.com` | `Cliente123!` |
| **Cliente 2** | `cliente2@banco.com` | `Cliente123!` |

---

## Funcionalidades Implementadas por Pregunta

### 1. Modelo de Datos y Migraciones (`feature/modelos-migracion`)
- Entidades `Cliente` y `SolicitudCredito` con enum `EstadoSolicitud` (`Pendiente`, `Aprobado`, `Rechazado`).
- Mapeos de tipo de datos (decimales, cadenas de texto) y relaciones FK mediante Fluent API.
- Migración inicial ejecutada sobre SQLite.

### 2. Autenticación y Seeding (`feature/roles-seeding`)
- Configuración de ASP.NET Core Identity.
- Seeding inicial en `Program.cs` para crear los roles, el usuario Analista y clientes con sus respectivos ingresos y solicitudes base.

### 3. Módulo de Solicitudes y Filtros (`feature/modulo-solicitudes`)
- Formulario de creación de solicitudes con reglas de negocio:
  - Validación de monto no mayor a 10 veces los ingresos del cliente.
  - Bloqueo de solicitudes si el cliente posee una solicitud activa en estado `Pendiente` o cuenta inactiva.
- Filtros avanzados en "Mis Solicitudes" por estado, rango de montos y fechas.

### 4. Cache con Redis y Sesión (`feature/sesion-redis`)
- Integración de `IDistributedCache` con Redis para almacenar el listado de solicitudes por 60 segundos.
- Invalidación automática de cache al registrar o actualizar solicitudes.
- Almacenamiento en `HttpContext.Session` de la última solicitud visitada, mostrando un acceso directo dinámico en el menú superior navigation bar.

### 5. Panel de Analista de Riesgo (`feature/panel-analista`)
- Control de acceso exclusivo para el rol `Analista` (`[Authorize(Roles = "Analista")]`).
- Panel de evaluación para revisar solicitudes `Pendientes`.
- Validación de Aprobación: No permite aprobar solicitudes que excedan 5 veces el ingreso mensual del cliente.
- Validación de Rechazo: Exige de forma obligatoria registrar un `MotivoRechazo`.
- Invalidación inmediata de la cache Redis del cliente tras el cambio de estado.

### 6. Contenerización y Despliegue (`deploy/render`)
- Configuración de `Dockerfile` multi-stage para .NET 8.
- Preparación del entorno para despliegue en la plataforma Render.com.

---

## Instrucciones para Ejecución Local

1. **Clonar el repositorio:**
   ```bash
   git clone [https://github.com/DiArGiEs/Examen-Parcial.git](https://github.com/DiArGiEs/Examen-Parcial.git)
   cd Examen-Parcial