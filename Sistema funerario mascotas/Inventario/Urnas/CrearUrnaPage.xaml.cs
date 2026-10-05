using Microsoft.Win32;
using Sistema_funerario_mascotas.Views.Clients;
using Sistema_funerario_mascotas.Views.Inventario.Urnas;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Sistema_funerario_mascotas.Views.Inventario.Urnas
{
    public partial class CrearUrnaPage : Page
    {
        private string _rutaImagenSeleccionada = string.Empty;

        public CrearUrnaPage()
        {
            InitializeComponent();
        }

        private void BtnSeleccionarImagen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar imagen de urna",
                Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _rutaImagenSeleccionada = openFileDialog.FileName;
                TxtRutaArchivo.Text = Path.GetFileName(_rutaImagenSeleccionada);

                // Mostrar vista previa local
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_rutaImagenSeleccionada);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImgPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo cargar la vista previa: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void GuardarUrna_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtErrorCodigo.Visibility = Visibility.Collapsed;
                TxtErrorNombre.Visibility = Visibility.Collapsed;
                TxtErrorPrecioCompra.Visibility = Visibility.Collapsed;
                TxtErrorPrecioVenta.Visibility = Visibility.Collapsed;

                bool hayErrores = false;

                if (string.IsNullOrWhiteSpace(TxtCodigo.Text))
                {
                    TxtErrorCodigo.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (string.IsNullOrWhiteSpace(TxtNombre.Text))
                {
                    TxtErrorNombre.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (!decimal.TryParse(TxtPrecioCompra.Text, out decimal precioCompra) || precioCompra < 0)
                {
                    TxtErrorPrecioCompra.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (!decimal.TryParse(TxtPrecioVenta.Text, out decimal precioVenta) || precioVenta < 0)
                {
                    TxtErrorPrecioVenta.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                int.TryParse(TxtExistencias.Text, out int existencias);
                int.TryParse(TxtStockMinimo.Text, out int stockMinimo);

                if (hayErrores) return;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(TxtCodigo.Text.Trim()), "codigo");
                formData.Add(new StringContent(TxtNombre.Text.Trim()), "nombre");

                if (!string.IsNullOrWhiteSpace(TxtDescripcion.Text))
                    formData.Add(new StringContent(TxtDescripcion.Text.Trim()), "descripcion");

                formData.Add(new StringContent(precioCompra.ToString()), "precio_compra");
                formData.Add(new StringContent(precioVenta.ToString()), "precio_venta");
                formData.Add(new StringContent(existencias.ToString()), "existencias");
                formData.Add(new StringContent((stockMinimo > 0 ? stockMinimo : 2).ToString()), "stock_minimo");

                // Adjuntar archivo de imagen si el usuario seleccionó uno
                if (!string.IsNullOrEmpty(_rutaImagenSeleccionada) && File.Exists(_rutaImagenSeleccionada))
                {
                    byte[] fileBytes = File.ReadAllBytes(_rutaImagenSeleccionada);
                    var fileContent = new ByteArrayContent(fileBytes);
                    formData.Add(fileContent, "imagen", Path.GetFileName(_rutaImagenSeleccionada));
                }

                 var response = await client.PostAsync($"{ApiConfig.BaseUrl}urnas", formData);
                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbar("Éxito", "Urna registrada correctamente.", true);
                    await System.Threading.Tasks.Task.Delay(1000);
                    this.NavigationService.Navigate(new UrnasPage());
                }
                else
                {
                    // Imprime el error exacto que envía Laravel para depurar rápidamente
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Detalle del error del servidor:\n{errorContent}", "Error 422 / 500", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BreadcrumbUrnas_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new UrnasPage());
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
                this.NavigationService.Navigate(new UrnasPage());
            }
        }
    }
}