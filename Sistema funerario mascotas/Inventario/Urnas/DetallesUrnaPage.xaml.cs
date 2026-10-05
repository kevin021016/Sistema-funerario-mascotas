using Sistema_funerario_mascotas.Views.Inventario.Urnas;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Sistema_funerario_mascotas.Views.Inventario.Urnas
{
    public partial class DetallesUrnaPage : Page
    {
        private int _urnaId;

        public DetallesUrnaPage(int urnaId)
        {
            InitializeComponent();
            _urnaId = urnaId;
            CargarDetallesUrna();
        }

        private async void CargarDetallesUrna()
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

                    // Configuramos el lector para aceptar números como strings
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var resultado = JsonSerializer.Deserialize<UrnaDetalleCompletaResponse>(json, opciones);

                    if (resultado != null && resultado.urna != null)
                    {
                        var urna = resultado.urna;
                        TxtTituloCodigo.Text = $"Detalles de Urna: {urna.codigo}";
                        LblCodigoUrna.Text = urna.codigo;
                        LblNombreUrna.Text = urna.nombre;
                        LblDescripcion.Text = string.IsNullOrWhiteSpace(urna.descripcion) ? "Sin descripción registrada." : urna.descripcion;

                        LblExistencias.Text = urna.existencias.ToString();
                        LblStockMinimo.Text = urna.stock_minimo.ToString();
                        LblPrecioVenta.Text = $"${urna.precio_venta:N2}";

                        LblPrecioCompra.Text = $"${urna.precio_compra:N2}";
                        LblCostoVenta.Text = $"${urna.precio_venta:N2}";

                        decimal margen = urna.precio_venta - urna.precio_compra;
                        LblMargenGanancia.Text = $"${margen:N2}";

                        if (!string.IsNullOrEmpty(urna.imagen_url))
                        {
                            try { ImgUrnaDetalle.Source = new BitmapImage(new Uri(urna.imagen_url)); } catch { }
                        }

                        if (resultado.historial != null && resultado.historial.Count > 0)
                        {
                            IcHistorialUrna.ItemsSource = resultado.historial;
                            LblSinHistorial.Visibility = Visibility.Collapsed;
                            IcHistorialUrna.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            IcHistorialUrna.Visibility = Visibility.Collapsed;
                            LblSinHistorial.Visibility = Visibility.Visible;
                        }
                    }
                }

                PanelLoader.Visibility = Visibility.Collapsed;
                GridContenidoPrincipal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar los detalles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new EditarUrnaPage(_urnaId));
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new UrnasPage());
        }

        private void BreadcrumbUrnas_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new UrnasPage());
        }

        public class UrnaDetalleCompletaResponse
        {
            public UrnaModel urna { get; set; }
            public List<ActivityLogModel> historial { get; set; } = new();
        }

        public class ActivityLogModel
        {
            public int id { get; set; }
            public string action { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
            public string created_at { get; set; } = string.Empty;
        }
    }
}