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
    public partial class ReportesUrnasPage : Page
    {
        private List<UrnaReporteModel> _todasLasUrnas = new();

        public ReportesUrnasPage()
        {
            InitializeComponent();
            CargarUrnasCompletas();
        }

        private async void CargarUrnasCompletas()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcUrnasReporte.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}urnas/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    _todasLasUrnas = JsonSerializer.Deserialize<List<UrnaReporteModel>>(json, opciones) ?? new List<UrnaReporteModel>();

                    AplicarFiltrosYMostrar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte de urnas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcUrnasReporte.Visibility = Visibility.Visible;
            }
        }

        private void AplicarFiltrosYMostrar()
        {
            var filtrados = _todasLasUrnas.AsEnumerable();

            string textoBusqueda = TxtBuscador.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                filtrados = filtrados.Where(u =>
                    (!string.IsNullOrEmpty(u.nombre) && u.nombre.ToLower().Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(u.codigo) && u.codigo.ToLower().Contains(textoBusqueda))
                );
            }

            if (DateTime.TryParseExact(TxtFechaDesde.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaDesde))
            {
                filtrados = filtrados.Where(u => DateTime.TryParse(u.created_at, out DateTime fU) && fU.Date >= fechaDesde.Date);
            }

            if (DateTime.TryParseExact(TxtFechaHasta.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaHasta))
            {
                filtrados = filtrados.Where(u => DateTime.TryParse(u.created_at, out DateTime fU) && fU.Date <= fechaHasta.Date);
            }

            var listaFinal = filtrados.ToList();

            IcUrnasReporte.ItemsSource = listaFinal;
            LblTotalModelos.Text = listaFinal.Count.ToString();
            LblTotalConteo.Text = listaFinal.Sum(u => u.existencias).ToString();
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

    public class UrnaReporteModel
    {
        public int id { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio_compra { get; set; }
        public decimal precio_venta { get; set; }
        public int existencias { get; set; }
        public string imagen { get; set; } = string.Empty;
        public string created_at { get; set; } = string.Empty;
        public string updated_at { get; set; } = string.Empty;

        public string precio_compra_formato => $"${precio_compra:N2}";
        public string precio_venta_formato => $"${precio_venta:N2}";

        // Corrección de la URL base para asegurar que apunte correctamente al dominio de Railway o local
        public string imagen_url => string.IsNullOrWhiteSpace(imagen) || imagen.Equals("img/urna.jpg", StringComparison.OrdinalIgnoreCase)
            ? $"{ApiConfig.BaseUrl.Replace("api/", "")}img/urna.jpg"
            : $"{ApiConfig.BaseUrl.Replace("api/", "")}{imagen}";

        public string fecha_creacion_formato => DateTime.TryParse(created_at, out DateTime dt) ? dt.ToString("dd/MM/yyyy") : created_at;

        public string fecha_modificacion_formato => DateTime.TryParse(updated_at, out DateTime dt) ? dt.ToString("dd/MM/yyyy HH:mm") : "-";
    }
}