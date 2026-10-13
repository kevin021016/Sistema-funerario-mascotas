using Sistema_funerario_mascotas.Views;
using Sistema_funerario_mascotas.Views.Clients;
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
    public partial class ReportesClientesPage : Page
    {
        private List<ClienteReporteModel> _todosLosClientes = new();
        private List<int> _coloniaIdsSeleccionadas = new();

        public ReportesClientesPage()
        {
            InitializeComponent();
            CargarClientesCompletos();
        }

        private async void CargarClientesCompletos()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcClientesReporte.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}clients/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    _todosLosClientes = JsonSerializer.Deserialize<List<ClienteReporteModel>>(json, opciones) ?? new List<ClienteReporteModel>();

                    AplicarFiltrosYMostrar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte de clientes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcClientesReporte.Visibility = Visibility.Visible;
            }
        }

        private void AbrirSelectorColonia_Click(object sender, RoutedEventArgs e)
        {
            var selectorColonia = new SelectorColoniaWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (selectorColonia.ShowDialog() == true && selectorColonia.ColoniaSeleccionada != null)
            {
                var col = selectorColonia.ColoniaSeleccionada;
                if (!_coloniaIdsSeleccionadas.Contains(col.id))
                {
                    _coloniaIdsSeleccionadas.Add(col.id);
                }

                TxtColoniaFiltro.Text = $"{_coloniaIdsSeleccionadas.Count} colonia(s)";
            }
        }

        private void LimpiarColonias_Click(object sender, RoutedEventArgs e)
        {
            _coloniaIdsSeleccionadas.Clear();
            TxtColoniaFiltro.Text = string.Empty;
            AplicarFiltrosYMostrar();
        }

        private void AplicarFiltrosYMostrar()
        {
            var filtrados = _todosLosClientes.AsEnumerable();

            // 1. Filtro por Buscador General (nombre, apellidos, teléfono o celular)
            string textoBusqueda = TxtBuscadorGeneral.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                filtrados = filtrados.Where(c =>
                    (!string.IsNullOrEmpty(c.nombre) && c.nombre.ToLower().Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(c.apellidos) && c.apellidos.ToLower().Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(c.telefono) && c.telefono.Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(c.celular) && c.celular.Contains(textoBusqueda))
                );
            }

            // 2. Filtro por Colonias (1 o más)
            if (_coloniaIdsSeleccionadas.Count > 0)
            {
                filtrados = filtrados.Where(c => c.colonia != null && _coloniaIdsSeleccionadas.Contains(c.colonia.id));
            }

            // 3. Filtro por Fechas
            if (DateTime.TryParseExact(TxtFechaDesde.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaDesde))
            {
                filtrados = filtrados.Where(c => DateTime.TryParse(c.created_at, out DateTime fC) && fC.Date >= fechaDesde.Date);
            }

            if (DateTime.TryParseExact(TxtFechaHasta.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaHasta))
            {
                filtrados = filtrados.Where(c => DateTime.TryParse(c.created_at, out DateTime fC) && fC.Date <= fechaHasta.Date);
            }

            var listaFinal = filtrados.ToList();

            // Actualizar UI y Métricas
            IcClientesReporte.ItemsSource = listaFinal;
            LblTotalClientes.Text = listaFinal.Count.ToString();
            LblTotalColonias.Text = listaFinal.Select(c => c.id).Where(id => id > 0).Distinct().Count().ToString();
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

    public class ClienteReporteModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public string celular { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public string ciudad { get; set; } = string.Empty;
        public ColoniaRelacionModel colonia { get; set; }
        public string created_at { get; set; } = string.Empty;

        public string colonia_nombre => colonia != null ? colonia.nombre : "Sin colonia";
    }

    public class ColoniaRelacionModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string codigo_postal { get; set; } = string.Empty;
    }
}