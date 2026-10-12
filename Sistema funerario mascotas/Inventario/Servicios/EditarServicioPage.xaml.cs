using Sistema_funerario_mascotas.Views;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Inventario.Servicios
{
    public partial class EditarServicioPage : Page
    {
        private int _servicioId;

        public EditarServicioPage(int servicioId)
        {
            InitializeComponent();
            _servicioId = servicioId;
            CargarDatosServicio();
        }

        private async void CargarDatosServicio()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                GridContenidoPrincipal.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}servicios/{_servicioId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString,
                        Converters = { new JsonStringBooleanConverter() }
                    };

                    var servicio = JsonSerializer.Deserialize<ServicioItemModel>(json, opciones);

                    if (servicio != null)
                    {
                        TxtTituloServicio.Text = $"Editar Servicio: {servicio.nombre}";
                        TxtNombre.Text = servicio.nombre;
                        TxtDescripcion.Text = servicio.descripcion;
                        TxtPrecio.Text = servicio.precio.ToString("F2");
                        ChkActivo.IsChecked = servicio.activo;
                    }
                }

                PanelLoader.Visibility = Visibility.Collapsed;
                GridContenidoPrincipal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar el servicio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ActualizarServicio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtErrorNombre.Visibility = Visibility.Collapsed;
                TxtErrorPrecio.Visibility = Visibility.Collapsed;

                bool hayErrores = false;

                if (string.IsNullOrWhiteSpace(TxtNombre.Text))
                {
                    TxtErrorNombre.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (!decimal.TryParse(TxtPrecio.Text.Trim(), out decimal precio) || precio < 0)
                {
                    TxtErrorPrecio.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (hayErrores) return;

                var payload = new
                {
                    nombre = TxtNombre.Text.Trim(),
                    descripcion = string.IsNullOrWhiteSpace(TxtDescripcion.Text) ? null : TxtDescripcion.Text.Trim(),
                    precio = precio,
                    activo = ChkActivo.IsChecked ?? true
                };

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // Usamos PUT para la actualización en el controlador de Laravel
                var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiConfig.BaseUrl}servicios/{_servicioId}")
                {
                    Content = jsonContent
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbar("Éxito", "Servicio actualizado correctamente.", true);
                    await Task.Delay(1000);
                    this.NavigationService.Navigate(new ServiciosPage());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error al actualizar: {errorContent}", "Error del servidor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BreadcrumbServicios_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ServiciosPage());
        }

        private void BreadcrumbServicios_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ServiciosPage());
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
}