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
    public partial class ReportesUsuariosPage : Page
    {
        private List<UsuarioReporteModel> _todosLosUsuarios = new();

        public ReportesUsuariosPage()
        {
            InitializeComponent();
            CargarUsuariosCompletos();
        }

        private async void CargarUsuariosCompletos()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcUsuariosReporte.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                // Consumimos la ruta /all para no tener paginación
                var response = await client.GetAsync($"{ApiConfig.BaseUrl}users/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    _todosLosUsuarios = JsonSerializer.Deserialize<List<UsuarioReporteModel>>(json, opciones) ?? new List<UsuarioReporteModel>();

                    AplicarFiltrosYMostrar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el reporte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcUsuariosReporte.Visibility = Visibility.Visible;
            }
        }

        private void AplicarFiltrosYMostrar()
        {
            var filtrados = _todosLosUsuarios.AsEnumerable();

            // Filtro por Rol
            if (CbFiltroRol.SelectedItem is ComboBoxItem itemRol && itemRol.Content.ToString() != "Todos")
            {
                string rolSeleccionado = itemRol.Content.ToString();
                filtrados = filtrados.Where(u => string.Equals(u.rol, rolSeleccionado, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por Fechas (si se ingresaron de forma válida)
            if (DateTime.TryParseExact(TxtFechaDesde.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaDesde))
            {
                filtrados = filtrados.Where(u => DateTime.TryParse(u.created_at, out DateTime fU) && fU.Date >= fechaDesde.Date);
            }

            if (DateTime.TryParseExact(TxtFechaHasta.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaHasta))
            {
                filtrados = filtrados.Where(u => DateTime.TryParse(u.created_at, out DateTime fU) && fU.Date <= fechaHasta.Date);
            }

            var listaFinal = filtrados.ToList();

            // Actualizar UI y Métricas
            IcUsuariosReporte.ItemsSource = listaFinal;
            LblTotalRegistros.Text = listaFinal.Count.ToString();
            LblTotalAdmins.Text = listaFinal.Count(u => string.Equals(u.rol, "Administrador", StringComparison.OrdinalIgnoreCase)).ToString();
            LblTotalPersonal.Text = listaFinal.Count(u => string.Equals(u.rol, "Vendedor", StringComparison.OrdinalIgnoreCase)).ToString();
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

    public class UsuarioReporteModel
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public string rol { get; set; } = string.Empty;
        public string created_at { get; set; } = string.Empty;
    }
}