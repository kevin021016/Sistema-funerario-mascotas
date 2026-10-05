using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Inventario.Urnas
{
    public partial class UrnasPage : Page
    {
        private int _paginaActual = 1;
        private int _ultimaPagina = 1;
        private string _busquedaActual = string.Empty;

        public UrnasPage()
        {
            InitializeComponent();
            CargarUrnasPagina(_paginaActual);
        }

        private async void CargarUrnasPagina(int pagina)
        {
            try
            {
                // Mostramos el loader y ocultamos la lista mientras responde Railway
                PanelLoader.Visibility = Visibility.Visible;
                IcUrnas.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                string url = $"{ApiConfig.BaseUrl}urnas?page={pagina}";
                if (!string.IsNullOrWhiteSpace(_busquedaActual))
                {
                    url += $"&search={Uri.EscapeDataString(_busquedaActual)}";
                }

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var resultado = JsonSerializer.Deserialize<UrnasPaginationWrapper>(json, opciones);

                    if (resultado != null)
                    {
                        IcUrnas.ItemsSource = resultado.data;
                        _paginaActual = resultado.current_page;
                        _ultimaPagina = resultado.last_page;
                        TxtPaginaActual.Text = $"Página {_paginaActual} de {_ultimaPagina}";

                        BtnAnterior.IsEnabled = _paginaActual > 1;
                        BtnSiguiente.IsEnabled = _paginaActual < _ultimaPagina;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar urnas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ocultamos el loader y restauramos la visibilidad de la lista
                PanelLoader.Visibility = Visibility.Collapsed;
                IcUrnas.Visibility = Visibility.Visible;
            }
        }

        private async void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            _busquedaActual = TxtBuscador.Text.Trim();
            _paginaActual = 1;
            CargarUrnasPagina(_paginaActual);
        }

        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                CargarUrnasPagina(--_paginaActual);
            }
        }

        private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _ultimaPagina)
            {
                CargarUrnasPagina(++_paginaActual);
            }
        }

        private void IrAgregarUrna_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new CrearUrnaPage());
        }

        private void IrDetallesUrna_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UrnaModel urna)
            {
                this.NavigationService.Navigate(new DetallesUrnaPage(urna.id));
            }
        }

        private void IrEditarUrna_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UrnaModel urna)
            {
                this.NavigationService.Navigate(new EditarUrnaPage(urna.id));
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int clienteId)
            {
                var confirmWindow = new ConfirmDialogWindow("Confirmar Eliminación", "¿Estás seguro de que deseas eliminar esta urna?")
                {
                    Owner = Application.Current.MainWindow
                };

                confirmWindow.ShowDialog();

                if (confirmWindow.Confirmed)
                {
                    try
                    {
                        var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}urnas/{clienteId}");
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                        var response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            MostrarSnackbar("Error", "No se pudo eliminar la urna.", exito: false);
                            return;
                        }

                        MostrarSnackbar("Éxito", "Urna eliminada correctamente.", exito: true);
                        CargarUrnasPagina(_paginaActual);
                    }
                    catch (Exception ex)
                    {
                        MostrarSnackbar("Error", $"Error de conexión: {ex.Message}", exito: false);
                    }
                }
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para exportar PDF
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para exportar Excel
        }

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            // this.NavigationService.Navigate(new DashboardPage());
        }

        private async void MostrarSnackbar(string titulo, string mensaje, bool exito)
        {
            var snackbar = new Wpf.Ui.Controls.Snackbar(SnackbarHost)
            {
                Title = titulo,
                Content = mensaje,
                Appearance = exito ? Wpf.Ui.Controls.ControlAppearance.Dark : Wpf.Ui.Controls.ControlAppearance.Danger,
                Timeout = TimeSpan.FromSeconds(4)
            };
            await snackbar.ShowAsync();
        }
    }

    public class UrnasPaginationWrapper
    {
        public int current_page { get; set; }
        public List<UrnaModel> data { get; set; } = new();
        public int last_page { get; set; }
    }

    public class UrnaModel
    {
        public int id { get; set; }
        public string codigo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio_compra { get; set; }
        public decimal precio_venta { get; set; }
        public int existencias { get; set; }
        public int stock_minimo { get; set; }
        public string imagen { get; set; } = string.Empty;

        public string imagen_url => string.IsNullOrEmpty(imagen)
             ? "https://via.placeholder.com/150"
             : $"{ApiConfig.BaseUrl.Replace("api/", "")}{imagen.Replace("public/", "")}";
        public string precio_compra_formato => $"${precio_compra:N2}";
        public string precio_venta_formato => $"${precio_venta:N2}";
    }
}