using Microsoft.Win32;
using Sistema_funerario_mascotas.Views.Inventario.Urnas;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Sistema_funerario_mascotas.Views.Inventario.Urnas
{
    public partial class EditarUrnaPage : Page
    {
        private int _urnaId;
        private string _rutaImagenSeleccionada = string.Empty;

        public EditarUrnaPage(int urnaId)
        {
            InitializeComponent();
            _urnaId = urnaId;
            CargarDatosUrna();
        }

        private async void CargarDatosUrna()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                GridContenidoPrincipal.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}urnas/{_urnaId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Intentamos deserializar como respuesta completa (por si viene de detalles) o directamente como UrnaModel
                    UrnaModel urna = null;
                    try
                    {
                        var wrapper = JsonSerializer.Deserialize<UrnaDetalleCompletaResponseForEdit>(json, opciones);
                        if (wrapper != null && wrapper.urna != null)
                        {
                            urna = wrapper.urna;
                        }
                    }
                    catch
                    {
                        // Si falla, intentamos deserializar directamente como UrnaModel (por si se usa en otro flujo)
                        urna = JsonSerializer.Deserialize<UrnaModel>(json, opciones);
                    }

                    if (urna != null)
                    {
                        TxtTituloUrna.Text = $"Editar Urna: {urna.codigo}";
                        TxtCodigo.Text = urna.codigo;
                        TxtNombre.Text = urna.nombre;
                        TxtDescripcion.Text = urna.descripcion;
                        TxtPrecioCompra.Text = urna.precio_compra.ToString("F2");
                        TxtPrecioVenta.Text = urna.precio_venta.ToString("F2");
                        TxtExistencias.Text = urna.existencias.ToString();
                        TxtStockMinimo.Text = urna.stock_minimo.ToString();

                        if (!string.IsNullOrEmpty(urna.imagen_url))
                        {
                            try { ImgPreview.Source = new BitmapImage(new Uri(urna.imagen_url)); } catch { }
                        }
                    }
                }

                PanelLoader.Visibility = Visibility.Collapsed;
                GridContenidoPrincipal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar la urna: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Clase de apoyo local para mapear la respuesta envuelta
        private class UrnaDetalleCompletaResponseForEdit
        {
            public UrnaModel urna { get; set; }
        }

        private void BtnSeleccionarImagen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar nueva imagen de urna",
                Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _rutaImagenSeleccionada = openFileDialog.FileName;
                TxtRutaArchivo.Text = Path.GetFileName(_rutaImagenSeleccionada);

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

        private async void ActualizarUrna_Click(object sender, RoutedEventArgs e)
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

                // Laravel requiere _method = PUT al enviar MultipartFormDataContent en actualizaciones
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent("PUT"), "_method");
                formData.Add(new StringContent(TxtCodigo.Text.Trim()), "codigo");
                formData.Add(new StringContent(TxtNombre.Text.Trim()), "nombre");
                formData.Add(new StringContent(string.IsNullOrWhiteSpace(TxtDescripcion.Text) ? "" : TxtDescripcion.Text.Trim()), "descripcion");
                formData.Add(new StringContent(precioCompra.ToString()), "precio_compra");
                formData.Add(new StringContent(precioVenta.ToString()), "precio_venta");
                formData.Add(new StringContent(existencias.ToString()), "existencias");
                formData.Add(new StringContent(stockMinimo.ToString()), "stock_minimo");

                if (!string.IsNullOrEmpty(_rutaImagenSeleccionada) && File.Exists(_rutaImagenSeleccionada))
                {
                    byte[] fileBytes = File.ReadAllBytes(_rutaImagenSeleccionada);
                    var fileContent = new ByteArrayContent(fileBytes);
                    formData.Add(fileContent, "imagen", Path.GetFileName(_rutaImagenSeleccionada));
                }

                var response = await client.PostAsync($"{ApiConfig.BaseUrl}urnas/{_urnaId}", formData);
                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbarLocal("Éxito", "Urna actualizada correctamente.", true);
                    await System.Threading.Tasks.Task.Delay(1000);
                    this.NavigationService.Navigate(new UrnasPage());
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

        private void BreadcrumbUrnas_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new UrnasPage());
        }

        private async void MostrarSnackbarLocal(string titulo, string mensaje, bool exito)
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