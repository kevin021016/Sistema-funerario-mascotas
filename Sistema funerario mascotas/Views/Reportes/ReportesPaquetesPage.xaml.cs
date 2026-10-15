using Sistema_funerario_mascotas.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Reportes
{
    public partial class ReportesPaquetesPage : Page
    {
        private List<PaqueteReporteModel> _todosLosPaquetes = new();

        public ReportesPaquetesPage()
        {
            InitializeComponent();
            CargarPaquetesCompletos();
        }

        private async void CargarPaquetesCompletos()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcPaquetesReporte.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                // Consumimos la ruta /paquetes/all del PaqueteController
                var response = await client.GetAsync($"{ApiConfig.BaseUrl}paquetes/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
                    };

                    _todosLosPaquetes = JsonSerializer.Deserialize<List<PaqueteReporteModel>>(json, opciones) ?? new List<PaqueteReporteModel>();

                    AplicarFiltrosYMostrar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte de paquetes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcPaquetesReporte.Visibility = Visibility.Visible;
            }
        }

        private void AplicarFiltrosYMostrar()
        {
            var filtrados = _todosLosPaquetes.AsEnumerable();

            // 1. Buscador (Nombre o Descripción)
            string textoBusqueda = TxtBuscador.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                filtrados = filtrados.Where(p =>
                    (!string.IsNullOrEmpty(p.nombre) && p.nombre.ToLower().Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(p.descripcion) && p.descripcion.ToLower().Contains(textoBusqueda))
                );
            }

            // 2. Filtro por Estatus (Activos / Inactivos)
            if (CbFiltroEstatus.SelectedItem is ComboBoxItem itemEstatus)
            {
                string estatusSel = itemEstatus.Content.ToString();
                if (estatusSel == "Activos")
                {
                    filtrados = filtrados.Where(p => p.activo_bool);
                }
                else if (estatusSel == "Inactivos")
                {
                    filtrados = filtrados.Where(p => !p.activo_bool);
                }
            }

            // 3. Filtro por Rango de Fechas
            if (DateTime.TryParseExact(TxtFechaDesde.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaDesde))
            {
                filtrados = filtrados.Where(p => DateTime.TryParse(p.created_at, out DateTime fP) && fP.Date >= fechaDesde.Date);
            }

            if (DateTime.TryParseExact(TxtFechaHasta.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaHasta))
            {
                filtrados = filtrados.Where(p => DateTime.TryParse(p.created_at, out DateTime fP) && fP.Date <= fechaHasta.Date);
            }

            var listaFinal = filtrados.ToList();

            // Actualizar UI y Métricas
            IcPaquetesReporte.ItemsSource = listaFinal;
            LblTotalPaquetes.Text = listaFinal.Count.ToString();
            LblTotalActivos.Text = listaFinal.Count(p => p.activo_bool).ToString();
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

    public class PaqueteReporteModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio { get; set; }
        public int? urna_id { get; set; }

        // Mapeo flexible para evitar problemas con bool vs int (0/1) de la BD
        public JsonElement activo { get; set; }

        public UrnaAsociadaPaqueteModel urna { get; set; }
        public List<ServicioAsociadoPaqueteModel> servicios { get; set; } = new();

        public string created_at { get; set; } = string.Empty;
        public string updated_at { get; set; } = string.Empty;

        public bool activo_bool
        {
            get
            {
                if (activo.ValueKind == JsonValueKind.True) return true;
                if (activo.ValueKind == JsonValueKind.False) return false;
                if (activo.ValueKind == JsonValueKind.Number && activo.GetInt32() == 1) return true;
                return false;
            }
        }

        public string precio_formato => $"${precio:N2}";
        public string estatus_texto => activo_bool ? "Activo" : "Inactivo";

        public string urna_nombre => urna != null ? $"{urna.nombre} ({urna.codigo})" : "Sin urna";
        public string servicios_nombres => (servicios != null && servicios.Count > 0)
            ? string.Join(", ", servicios.Select(s => s.nombre))
            : "Sin servicios extras";

        public string fecha_creacion_formato => DateTime.TryParse(created_at, out DateTime dt) ? dt.ToString("dd/MM/yyyy") : created_at;
        public string fecha_modificacion_formato => DateTime.TryParse(updated_at, out DateTime dt) ? dt.ToString("dd/MM/yyyy HH:mm") : "-";
    }

    public class UrnaAsociadaPaqueteModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string codigo { get; set; } = string.Empty;
    }

    public class ServicioAsociadoPaqueteModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
    }
}