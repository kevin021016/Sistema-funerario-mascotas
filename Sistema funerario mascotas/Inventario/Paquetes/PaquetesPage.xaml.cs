using Sistema_funerario_mascotas.Views;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Inventario.Paquetes
{
    public partial class PaquetesPage : Page
    {
        private int _paginaActual = 1;
        private int _ultimaPagina = 1;
        private string _busquedaActual = string.Empty;

        public PaquetesPage()
        {
            InitializeComponent();
            CargarPaquetesPagina(_paginaActual);
        }

        private async void CargarPaquetesPagina(int pagina)
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                IcPaquetes.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                string url = $"{ApiConfig.BaseUrl}paquetes?page={pagina}";
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
                        NumberHandling = JsonNumberHandling.AllowReadingFromString,
                        Converters = { new JsonStringBooleanConverter() }
                    };

                    var resultado = JsonSerializer.Deserialize<PaquetesPaginationWrapper>(json, opciones);

                    if (resultado != null)
                    {
                        IcPaquetes.ItemsSource = resultado.data;
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
                MessageBox.Show($"Error al cargar paquetes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                IcPaquetes.Visibility = Visibility.Visible;
            }
        }

        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            _busquedaActual = TxtBuscador.Text.Trim();
            _paginaActual = 1;
            CargarPaquetesPagina(_paginaActual);
        }

        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                CargarPaquetesPagina(--_paginaActual);
            }
        }

        private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _ultimaPagina)
            {
                CargarPaquetesPagina(++_paginaActual);
            }
        }

        private void IrAgregarPaquete_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new CrearPaquetePage());
        }

        private void IrEditarPaquete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PaqueteItemModel paquete)
            {
                // this.NavigationService.Navigate(new EditarPaquetePage(paquete.id));
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int paqueteId)
            {
                var confirmWindow = new ConfirmDialogWindow("Confirmar Eliminación", "¿Estás seguro de que deseas eliminar este paquete?")
                {
                    Owner = Application.Current.MainWindow
                };

                confirmWindow.ShowDialog();

                if (confirmWindow.Confirmed)
                {
                    try
                    {
                        var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}paquetes/{paqueteId}");
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                        var response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            MostrarSnackbar("Error", "No se pudo eliminar el paquete.", exito: false);
                            return;
                        }

                        MostrarSnackbar("Éxito", "Paquete eliminado correctamente.", exito: true);
                        CargarPaquetesPagina(_paginaActual);
                    }
                    catch (Exception ex)
                    {
                        MostrarSnackbar("Error", $"Error de conexión: {ex.Message}", exito: false);
                    }
                }
            }
        }

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(DashboardPage));
            }
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

    public class PaquetesPaginationWrapper
    {
        public int current_page { get; set; }
        public List<PaqueteItemModel> data { get; set; } = new();
        public int last_page { get; set; }
    }

    public class PaqueteItemModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio { get; set; }
        public int? urna_id { get; set; }
        public UrnaAsociadaModel urna { get; set; }
        public bool activo { get; set; }

        public string precio_formato => $"${precio:N2}";
        public string urna_nombre_formato => urna != null ? $"{urna.nombre}" : "Ninguna";
        public string estatus_texto => activo ? "Activo" : "Inactivo";
    }

    public class UrnaAsociadaModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string codigo { get; set; } = string.Empty;
    }

    public class JsonStringBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False) return reader.GetBoolean();
            if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32() != 0;
            if (reader.TokenType == JsonTokenType.String)
            {
                string s = reader.GetString();
                if (bool.TryParse(s, out bool b)) return b;
                if (int.TryParse(s, out int i)) return i != 0;
                if (s == "1" || string.Equals(s, "active", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
    }
}