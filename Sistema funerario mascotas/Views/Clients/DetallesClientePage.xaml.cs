using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Clients
{
    public partial class DetallesClientePage : Page
    {
        private int _clienteId;

        public DetallesClientePage(int clienteId)
        {
            InitializeComponent();
            _clienteId = clienteId;
            CargarExpedienteCliente();
        }

        private async void CargarExpedienteCliente()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                GridContenidoPrincipal.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}clients/{_clienteId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    
                    var resultado = JsonSerializer.Deserialize<ClienteDetalleCompletaResponse>(json, opciones);

                    if (resultado != null && resultado.cliente != null)
                    {
                        var cliente = resultado.cliente;
                        
                        // Corrección de comillas dobles e interpolación
                        TxtTituloNombre.Text = $"Expediente: {cliente.nombre}";
                        LblNombreCliente.Text = cliente.nombre;
                        LblTelefono.Text = string.IsNullOrWhiteSpace(cliente.telefono) ? "No registrado" : cliente.telefono;
                        LblEmail.Text = string.IsNullOrWhiteSpace(cliente.email) ? "No registrado" : cliente.email;
                        LblDireccion.Text = string.IsNullOrWhiteSpace(cliente.direccion) ? "Sin dirección registrada." : cliente.direccion;

                        // Widgets y métricas financieras
                        int totalContratos = resultado.contratos != null ? resultado.contratos.Count : 0;
                        LblTotalContratos.Text = totalContratos.ToString();

                        decimal inversionTotal = 0;
                        if (resultado.contratos != null)
                        {
                            foreach (var c in resultado.contratos)
                            {
                                inversionTotal += c.monto_final;
                            }
                        }
                        LblInversionAcumulada.Text = $"${inversionTotal:N2}";
                        LblEstatusCliente.Text = totalContratos > 0 ? "Cliente Frecuente" : "Nuevo";

                        // Poblar tabla de contratos
                        if (resultado.contratos != null && resultado.contratos.Count > 0)
                        {
                            IcContratosCliente.ItemsSource = resultado.contratos;
                            LblSinContratos.Visibility = Visibility.Collapsed;
                            IcContratosCliente.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            IcContratosCliente.Visibility = Visibility.Collapsed;
                            LblSinContratos.Visibility = Visibility.Visible;
                        }
                    }
                }

                PanelLoader.Visibility = Visibility.Collapsed;
                GridContenidoPrincipal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar el expediente del cliente: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IrDetallesContrato_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ContratoItemModel contrato)
            {
                // this.NavigationService.Navigate(new DetallesContratoPage(contrato.id));
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ClientesPage());
        }

        private void BreadcrumbClientes_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ClientesPage());
        }

        public class ClienteDetalleCompletaResponse
        {
            public ClienteModel cliente { get; set; }
            public List<ContratoItemModel> contratos { get; set; } = new();
        }

        public class ClienteModel
        {
            public int id { get; set; }
            public string nombre { get; set; } = string.Empty;
            public string telefono { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
            public string direccion { get; set; } = string.Empty;
        }

        public class ContratoItemModel
        {
            public int id { get; set; }
            public string folio { get; set; } = string.Empty;
            public string mascota_nombre { get; set; } = string.Empty;
            public decimal monto_final { get; set; }
            public string estatus { get; set; } = string.Empty;

            public string monto_final_formato => $"${monto_final:N2}";
        }
    }
}