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
    public partial class ReportesServiciosPage : Page
    {
        private List<ServicioReporteModel> _todosLosServicios = new();

        public ReportesServiciosPage()
        {
            InitializeComponent();
            CargarServiciosCompletos();
        }

        private async void CargarServiciosCompletos()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcServiciosReporte.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}servicios/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
                    };

                    _todosLosServicios = JsonSerializer.Deserialize<List<ServicioReporteModel>>(json, opciones) ?? new List<ServicioReporteModel>();

                    AplicarFiltrosYMostrar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte de servicios: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcServiciosReporte.Visibility = Visibility.Visible;
            }
        }

        private void AplicarFiltrosYMostrar()
        {
            var filtrados = _todosLosServicios.AsEnumerable();

            // 1. Buscador (Nombre o Descripción)
            string textoBusqueda = TxtBuscador.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                filtrados = filtrados.Where(s =>
                    (!string.IsNullOrEmpty(s.nombre) && s.nombre.ToLower().Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(s.descripcion) && s.descripcion.ToLower().Contains(textoBusqueda))
                );
            }

            // 2. Filtro por Estatus (Activos / Inactivos)
            if (CbFiltroEstatus.SelectedItem is ComboBoxItem itemEstatus)
            {
                string estatusSel = itemEstatus.Content.ToString();
                if (estatusSel == "Activos")
                {
                    filtrados = filtrados.Where(s => s.activo_bool);
                }
                else if (estatusSel == "Inactivos")
                {
                    filtrados = filtrados.Where(s => !s.activo_bool);
                }
            }

            // 3. Filtro por Rango de Fechas
            if (DateTime.TryParseExact(TxtFechaDesde.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaDesde))
            {
                filtrados = filtrados.Where(s => DateTime.TryParse(s.created_at, out DateTime fS) && fS.Date >= fechaDesde.Date);
            }

            if (DateTime.TryParseExact(TxtFechaHasta.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaHasta))
            {
                filtrados = filtrados.Where(s => DateTime.TryParse(s.created_at, out DateTime fS) && fS.Date <= fechaHasta.Date);
            }

            var listaFinal = filtrados.ToList();

            // Actualizar UI y Métricas
            IcServiciosReporte.ItemsSource = listaFinal;
            LblTotalServicios.Text = listaFinal.Count.ToString();
            LblTotalActivos.Text = listaFinal.Count(s => s.activo_bool).ToString();
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

    public class ServicioReporteModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio { get; set; }

        // Mapeo flexible para evitar el error de System.Boolean vs Número
        public JsonElement activo { get; set; }

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

        public string fecha_creacion_formato => DateTime.TryParse(created_at, out DateTime dt) ? dt.ToString("dd/MM/yyyy") : created_at;
        public string fecha_modificacion_formato => DateTime.TryParse(updated_at, out DateTime dt) ? dt.ToString("dd/MM/yyyy HH:mm") : "-";
    }
}