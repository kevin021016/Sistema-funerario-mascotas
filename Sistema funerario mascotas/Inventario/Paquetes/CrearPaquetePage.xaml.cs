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
    public partial class CrearPaquetePage : Page
    {
        public CrearPaquetePage()
        {
            InitializeComponent();
            CargarUrnasDropdown();
        }

        private async void CargarUrnasDropdown()
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}urnas");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var listaUrnas = ParsearListaUrnas(json, opciones);
                    listaUrnas.Insert(0, new UrnaDropdownModel { id = 0, nombre = "Ninguna urna asociada" });

                    CbUrnas.ItemsSource = listaUrnas;
                    CbUrnas.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar urnas: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private async void GuardarPaquete_Click(object sender, RoutedEventArgs e)
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
                var response = await client.PostAsync($"{ApiConfig.BaseUrl}paquetes", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbar("Éxito", "Paquete creado exitosamente.", true);
                    await Task.Delay(1000);
                    this.NavigationService.Navigate(new PaquetesPage());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error al guardar: {errorContent}", "Error del servidor", MessageBoxButton.OK, MessageBoxImage.Error);
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

    public class UrnaDropdownModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string codigo { get; set; } = string.Empty;
        public string nombre_display => id == 0 ? nombre : $"{nombre} ({codigo})";
    }

    public class UrnaPaginationWrapper
    {
        public List<UrnaDropdownModel> data { get; set; } = new();
    }
}