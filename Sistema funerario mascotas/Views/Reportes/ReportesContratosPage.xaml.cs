using Sistema_funerario_mascotas.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Reportes
{
    public partial class ReportesContratosPage : Page
    {
        private List<ContratoReporteModel> _todosLosContratos = new();

        public ReportesContratosPage()
        {
            InitializeComponent();
            CargarContratosCompletos();
        }

        private async void CargarContratosCompletos()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcContratosReporte.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                // Consumimos la ruta /all para obtener la lista completa sin paginación
                var response = await client.GetAsync($"{ApiConfig.BaseUrl}contratos/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    _todosLosContratos = JsonSerializer.Deserialize<List<ContratoReporteModel>>(json, opciones) ?? new List<ContratoReporteModel>();

                    AplicarFiltrosYMostrar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte de contratos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcContratosReporte.Visibility = Visibility.Visible;
            }
        }

        // Acepta 01/09/2026 y 1/9/2026. Devuelve false si el texto no está vacío pero tampoco es una fecha válida.
        private static bool TryLeerFechaFiltro(string texto, out DateTime? fecha)
        {
            fecha = null;
            texto = (texto ?? string.Empty).Trim();
            if (texto.Length == 0) return true;

            if (DateTime.TryParseExact(texto, new[] { "dd/MM/yyyy", "d/M/yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            {
                fecha = d;
                return true;
            }
            return false;
        }

        private void AplicarFiltrosYMostrar()
        {
            // Validar fechas antes de filtrar para no ignorar en silencio un texto inválido
            if (!TryLeerFechaFiltro(TxtFechaDesde.Text, out DateTime? fechaDesde) ||
                !TryLeerFechaFiltro(TxtFechaHasta.Text, out DateTime? fechaHasta))
            {
                MessageBox.Show("Una de las fechas no es válida. Usa el formato DD/MM/AAAA.", "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var filtrados = _todosLosContratos.AsEnumerable();

            // 1. Buscador (Folio, Cliente o Mascota), sin distinguir mayúsculas ni acentos
            string textoBusqueda = ReporteTexto.Normalizar(TxtBuscador.Text);
            if (textoBusqueda.Length > 0)
            {
                filtrados = filtrados.Where(c =>
                    ReporteTexto.Normalizar(c.folio).Contains(textoBusqueda) ||
                    ReporteTexto.Normalizar(c.cliente_nombre_completo).Contains(textoBusqueda) ||
                    ReporteTexto.Normalizar(c.mascota_nombre).Contains(textoBusqueda));
            }

            // 2. Estatus
            if (CbFiltroEstatus.SelectedItem is ComboBoxItem itemEstatus && itemEstatus.Content.ToString() != "Todos")
            {
                string estatusSel = ReporteTexto.Normalizar(itemEstatus.Content.ToString());
                filtrados = filtrados.Where(c => ReporteTexto.Normalizar(c.estatus) == estatusSel);
            }

            // 3. Tipo de pago (Contado / Crédito) -> campo tipo_pago
            if (CbFiltroTipoPago.SelectedItem is ComboBoxItem itemTipo && itemTipo.Content.ToString() != "Todos")
            {
                string tipoSel = ReporteTexto.Normalizar(itemTipo.Content.ToString());
                filtrados = filtrados.Where(c => ReporteTexto.Normalizar(c.tipo_pago) == tipoSel);
            }

            // 4. Modalidad (Semanal / Quincenal / Mensual) -> campo frecuencia_pago, solo contratos a crédito
            if (CbFiltroModalidad.SelectedItem is ComboBoxItem itemMod && itemMod.Content.ToString() != "Todas")
            {
                string modalidadSel = ReporteTexto.Normalizar(itemMod.Content.ToString());
                filtrados = filtrados.Where(c => !c.es_contado && ReporteTexto.Normalizar(c.frecuencia_pago) == modalidadSel);
            }

            // 5. Rango de fechas (sobre la fecha de venta)
            if (fechaDesde.HasValue)
            {
                filtrados = filtrados.Where(c => c.fecha_venta_dt.HasValue && c.fecha_venta_dt.Value.Date >= fechaDesde.Value.Date);
            }

            if (fechaHasta.HasValue)
            {
                filtrados = filtrados.Where(c => c.fecha_venta_dt.HasValue && c.fecha_venta_dt.Value.Date <= fechaHasta.Value.Date);
            }

            var listaFinal = filtrados.ToList();

            // Actualizar UI y Métricas
            IcContratosReporte.ItemsSource = listaFinal;
            LblTotalContratos.Text = listaFinal.Count.ToString();

            decimal totalRecaudado = listaFinal.Sum(c => c.monto_recaudado);
            decimal totalPendiente = listaFinal.Sum(c => c.monto_restante);

            LblMontoRecaudado.Text = $"${totalRecaudado:N2}";
            LblMontoPendiente.Text = $"${totalPendiente:N2}";
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            AplicarFiltrosYMostrar();
        }

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(DashboardPage));
            }
        }

        private void BreadcrumbReportes_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ReportesPage());
        }

        private void BreadcrumbReportes_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ReportesPage());
        }
    }

    // Utilidades de texto y fechas para los reportes
    internal static class ReporteTexto
    {
        // Minúsculas y sin acentos: "Crédito" == "credito"
        public static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Lee solo "yyyy-MM-dd" de lo que mande la API (con o sin hora/Z) para evitar el corrimiento por zona horaria
        public static DateTime? ParsearFechaApi(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Length < 10) return null;

            return DateTime.TryParseExact(texto.Substring(0, 10), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)
                ? d
                : (DateTime?)null;
        }

        public static string Capitalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            texto = texto.Trim();
            return char.ToUpperInvariant(texto[0]) + texto.Substring(1).ToLowerInvariant();
        }
    }

    public class ContratoReporteModel
    {
        public int id { get; set; }
        public string folio { get; set; } = string.Empty;
        public string fecha_venta { get; set; } = string.Empty;
        public string fecha_primer_pago { get; set; } = string.Empty;
        public string mascota_nombre { get; set; } = string.Empty;
        public string mascota_tipo { get; set; } = string.Empty;
        public decimal monto_final { get; set; }
        public decimal monto_restante { get; set; }
        public string modalidad { get; set; } = string.Empty;          // Oficina / Domicilio / Remoto
        public string tipo_pago { get; set; } = string.Empty;          // contado / credito
        public string frecuencia_pago { get; set; } = string.Empty;    // semanal / quincenal / mensual
        public string estatus { get; set; } = string.Empty;

        public ClienteAsociadoModel cliente { get; set; }
        public AsesorAsociadoModel asesor { get; set; }
        public List<PagoAsociadoModel> pagos { get; set; } = new();

        public string cliente_nombre_completo => cliente != null ? $"{cliente.nombre} {cliente.apellidos}" : "Sin cliente";
        public string mascota_nombre_tipo => $"{mascota_nombre} ({mascota_tipo})";
        public string asesor_nombre => asesor != null ? $"{asesor.name} {asesor.apellidos}" : "Sistema / Desconocido";

        // Fechas ya interpretadas (sin conversión de zona horaria) y formateadas para mostrar
        public DateTime? fecha_venta_dt => ReporteTexto.ParsearFechaApi(fecha_venta);
        public DateTime? fecha_primer_pago_dt => ReporteTexto.ParsearFechaApi(fecha_primer_pago);
        public string fecha_venta_formato => fecha_venta_dt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? fecha_venta;
        public string fecha_primer_pago_formato => fecha_primer_pago_dt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? fecha_primer_pago;

        // Tipo de pago y modalidad (periodicidad)
        public bool es_contado => ReporteTexto.Normalizar(tipo_pago) == "contado";
        public string tipo_pago_formato => ReporteTexto.Normalizar(tipo_pago) switch
        {
            "contado" => "Contado",
            "credito" => "Crédito",
            _ => ReporteTexto.Capitalizar(tipo_pago)
        };
        public string modalidad_formato => es_contado || string.IsNullOrWhiteSpace(frecuencia_pago)
            ? "—"
            : ReporteTexto.Capitalizar(frecuencia_pago);

        // Cálculo de lo recaudado basado en los pagos registrados
        public decimal monto_recaudado => pagos != null ? pagos.Sum(p => p.monto) : 0;

        public string monto_recaudado_formato => $"${monto_recaudado:N2}";
        public string monto_restante_formato => $"${monto_restante:N2}";
    }

    public class ClienteAsociadoModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
    }

    public class AsesorAsociadoModel
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
    }

    public class PagoAsociadoModel
    {
        public int id { get; set; }
        public decimal monto { get; set; }
        public string tipo_pago { get; set; } = string.Empty;
    }
}