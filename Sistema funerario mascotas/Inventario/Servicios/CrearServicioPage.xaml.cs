using Sistema_funerario_mascotas.Views;
using Sistema_funerario_mascotas.Views.Clients;
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
    public partial class CrearServicioPage : Page
    {
        public CrearServicioPage()
        {
            InitializeComponent();
        }

        private async void GuardarServicio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Limpiar errores visuales previos
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

                var response = await client.PostAsync($"{ApiConfig.BaseUrl}servicios", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbar("Éxito", "Servicio creado exitosamente.", true);
                    await Task.Delay(1000);
                    this.NavigationService.Navigate(new ServiciosPage());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error al guardar el servicio: {errorContent}", "Error del servidor", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                this.NavigationService.Navigate(new ServiciosPage());
            }
        }
    }
}