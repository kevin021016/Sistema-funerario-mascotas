<div align="center">

# 🐾 Sistema Funerario para Mascotas (Desktop)

> **Solución integral de escritorio para la administración operativa, inventario, control de clientes y seguimiento financiero de servicios funerarias para mascotas.**

![Status](https://img.shields.io/badge/Estado-En%20Desarrollo%20(WIP)-yellow?style=flat-square&logo=git&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-%235C2D91.svg?style=flat-square&logo=.net&logoColor=white)
![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=flat-square&logo=csharp&logoColor=white)
![Laravel](https://img.shields.io/badge/Laravel-%23FF2D20.svg?style=flat-square&logo=laravel&logoColor=white)

</div>

---

## 🏗️ Arquitectura y Tecnologías

El sistema está construido bajo una arquitectura desacoplada:
* **Frontend (Cliente de Escritorio):** Desarrollado en **C# / .NET** utilizando **WPF** y aplicando el diseño moderno de **WPF-UI**.
* **Backend (API):** Conectado a una **API REST en Laravel** para la gestión centralizada de datos y lógica de negocio.

---

## ✨ Módulos Implementados (Funcionalidades Actuales)

### 👤 Gestión de Clientes (`ClientesPage` / `DetallesClientePage`)
- Búsqueda y listado paginado de clientes.
- Visualización de expediente detallado con widgets de métricas financieras (total de contratos e inversión acumulada).
- Tabla interactiva integrada que lista todos los contratos funerarios asociados a cada cliente con acceso directo a sus detalles.

### 📦 Gestión de Inventario de Urnas (`UrnasPage` / `CrearUrnaPage` / `EditarUrnaPage` / `DetallesUrnaPage`)
- Control de existencias, precios de compra y venta, stock mínimo y cálculo automático de márgenes de ganancia estimada.
- Subida, almacenamiento y previsualización dinámica de fotografías de las urnas.
- Historial integrado de movimientos y auditoría por cada artículo (creaciones, actualizaciones, asignaciones).

### 📝 Gestión de Contratos Funerarios (`ContratosPage` / `DetallesContratoPage`)
- Expediente completo del contrato con datos del titular, especificaciones de la mascota (nombre, tipo, peso, edad) y servicios extras contratados.
- Resumen financiero de cuenta en tiempo real (subtotal, extras, monto final, enganche y saldo pendiente).
- **Módulo de Pagos y Abonos Integrado:** Tabla interactiva de abonos y enganches, opción para ver observaciones detalladas mediante alertas emergentes, eliminación de registros con devolución automática de saldos al contrato y ventana modal para registrar nuevos pagos.

---

## 🚀 Roadmap y Próximas Fases (WIP)

> ⚠️ **Nota:** Este proyecto se encuentra en desarrollo activo para mi portafolio profesional. Las siguientes características están planificadas para las próximas iteraciones:

### 1. Módulo Dashboard (`Dashboard`)
* **Objetivo:** Proveer una vista general analítica y de acceso rápido al estado operativo de la funeraria al iniciar la aplicación.
* **Características proyectadas:**
  - [ ] Diseño de la interfaz visual y estructuración del layout principal del dashboard.
  - [ ] Consumo e integración de endpoints de la API REST para métricas en tiempo real (ingresos del mes, contratos activos, alertas de stock bajo en urnas).
  - [ ] Widgets de accesos rápidos y gráficos estadísticos de resumen operativo.

### 2. Módulo de Paquetes (`Paquetes`)
* **Objetivo:** Administrar el catálogo general de servicios funerarios base que ofrece la empresa (cremación individual, colectiva, traslados, ceremonias).
* **Características proyectadas:**
  - [ ] CRUD completo de paquetes funerarios y sus precios base.
  - [ ] Asociación directa de servicios extras personalizables.
  - [ ] Selectores rápidos para la creación de nuevos contratos.

### 3. Módulo de Reportes e Impresión (`Reportes`)
* **Objetivo:** Generar informes operativos, gerenciales y financieros con capacidades de exportación e impresión directa.
* **Características clave pendientes:**
  - [ ] **Conexión directa con impresora:** Envío de reportes de cortes de caja, comprobantes de pago y tickets directamente a la impresora predeterminada sin pasar por vistas previas complejas.
  - [ ] **Filtros avanzados por fecha:** Selección de periodos personalizados (por rango de fechas, quincenas, mes actual) o consulta de histórico completo.
  - [ ] **Filtros por vendedor:** Capacidad de auditar y filtrar los reportes seleccionando un vendedor específico o consultando el consolidado general de todos los agentes.

---
