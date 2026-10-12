using Sistema_funerario_mascotas.Views;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Inventario.Paquetes
{
    public partial class EditarPaquetePage : Page
    {
        private int _paqueteId;

        public EditarPaquetePage(int paqueteId)
        {
            InitializeComponent();
            _paqueteId = paqueteId;
            CargarDatosYUrnas();
        }

        private async void CargarDatosYUrnas()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                GridContenidoPrincipal.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var opciones = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                    Converters = { new JsonStringBooleanConverter() }
                };

                // 1. Cargar catálogo de urnas para el ComboBox
                var resUrnas = await client.GetAsync($"{ApiConfig.BaseUrl}urnas");
                if (resUrnas.IsSuccessStatusCode)
                {
                    var jsonUrnas = await resUrnas.Content.ReadAsStringAsync();
                    var listaUrnas = ParsearListaUrnas(jsonUrnas, opciones);
                    listaUrnas.Insert(0, new UrnaDropdownModel { id = 0, nombre = "Ninguna urna asociada" });

                    CbUrnas.ItemsSource = listaUrnas;
                }

                // 2. Cargar datos del paquete actual
                var resPaquete = await client.GetAsync($"{ApiConfig.BaseUrl}paquetes/{_paqueteId}");
                if (resPaquete.IsSuccessStatusCode)
                {
                    var jsonPaquete = await resPaquete.Content.ReadAsStringAsync();
                    var paquete = JsonSerializer.Deserialize<PaqueteItemModel>(jsonPaquete, opciones);

                    if (paquete != null)
                    {
                        TxtTituloPaquete.Text = $"Editar Paquete: {paquete.nombre}";
                        TxtNombre.Text = paquete.nombre;
                        TxtDescripcion.Text = paquete.descripcion;
                        TxtPrecio.Text = paquete.precio.ToString("F2");
                        CbUrnas.SelectedValue = paquete.urna_id ?? 0;
                        ChkActivo.IsChecked = paquete.activo;
                    }
                }

                PanelLoader.Visibility = Visibility.Collapsed;
                GridContenidoPrincipal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar el paquete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<UrnaDropdownModel> ParsearListaUrnas(string json, JsonSerializerOptions opciones)
        {
            try
            {
                return JsonSerializer.Deserialize<List<UrnaDropdownModel>>(json, opciones) ?? new List<UrnaDropdownModel>();
            }
            catch
            {
                try
                {
                    var paginado = JsonSerializer.Deserialize<UrnaPaginationWrapper>(json, opciones);
                    return paginado?.data ?? new List<UrnaDropdownModel>();
                }
                catch
                {
                    return new List<UrnaDropdownModel>();
                }
            }
        }

        private async void ActualizarPaquete_Click(object sender, RoutedEventArgs e)
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

                int? urnaIdValido = (CbUrnas.SelectedValue != null && (int)CbUrnas.SelectedValue > 0) ? (int?)CbUrnas.SelectedValue : null;

                var payload = new
                {
                    nombre = TxtNombre.Text.Trim(),
                    descripcion = string.IsNullOrWhiteSpace(TxtDescripcion.Text) ? null : TxtDescripcion.Text.Trim(),
                    precio = precio,
                    urna_id = urnaIdValido,
                    activo = ChkActivo.IsChecked ?? true
                };

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiConfig.BaseUrl}paquetes/{_paqueteId}")
                {
                    Content = jsonContent
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbar("Éxito", "Paquete actualizado correctamente.", true);
                    await Task.Delay(1000);
                    this.NavigationService.Navigate(new PaquetesPage());
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

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new PaquetesPage());
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