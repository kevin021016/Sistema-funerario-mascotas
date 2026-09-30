using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Contratos
{
    public partial class DetallesContratoPage : Page
    {
        private int _contratoId;
        private ObservableCollection<ServicioExtraItemDetalle> _serviciosExtrasList = new();

        public DetallesContratoPage(int contratoId)
        {
            InitializeComponent();
            _contratoId = contratoId;
            IcServiciosExtrasDetalle.ItemsSource = _serviciosExtrasList;

            CargarDetallesContrato();
        }

        public DetallesContratoPage(ContratoModel contrato) : this(contrato.id) { }

        private async void CargarDetallesContrato()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                GridContenidoPrincipal.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var response = await client.GetAsync($"{ApiConfig.BaseUrl}contratos/{_contratoId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var contrato = JsonSerializer.Deserialize<ContratoDetalleModel>(json, opciones);

                    if (contrato != null)
                    {
                        TxtTituloFolio.Text = $"Contrato Folio: {contrato.folio}";
                        LblClienteNombre.Text = contrato.cliente != null ? $"{contrato.cliente.nombre} {contrato.cliente.apellidos}" : "Cliente no asignado";
                        LblClienteTelefono.Text = contrato.cliente != null ? (!string.IsNullOrEmpty(contrato.cliente.celular) ? contrato.cliente.celular : contrato.cliente.telefono) : "-";

                        LblMascotaNombre.Text = contrato.mascota_nombre;
                        LblMascotaTipo.Text = contrato.mascota_tipo;
                        LblMascotaPeso.Text = string.IsNullOrEmpty(contrato.mascota_peso) ? "No especificado" : contrato.mascota_peso;
                        LblMascotaEdad.Text = string.IsNullOrEmpty(contrato.mascota_edad) ? "No especificada" : contrato.mascota_edad;

                        LblPaqueteNombre.Text = contrato.paquete != null ? contrato.paquete.nombre : "Paquete personalizado / libre";
                        LblPaquetePrecio.Text = $"${contrato.precio_paquete_aplicado:N2}";
                        LblUrnaNombre.Text = contrato.urna != null ? contrato.urna.nombre : "Ninguna asignada";

                        // Servicios Extras
                        _serviciosExtrasList.Clear();
                        if (contrato.servicios_extras != null && contrato.servicios_extras.Count > 0)
                        {
                            foreach (var s in contrato.servicios_extras)
                            {
                                _serviciosExtrasList.Add(new ServicioExtraItemDetalle
                                {
                                    nombre = s.nombre ?? "Servicio extra",
                                    precio_aplicado = s.precio_aplicado
                                });
                            }
                            LblSinExtras.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            LblSinExtras.Visibility = Visibility.Visible;
                        }

                        // Esquema financiero y fechas
                        LblTipoPago.Text = char.ToUpper(contrato.tipo_pago[0]) + contrato.tipo_pago.Substring(1);
                        LblMetodoModalidad.Text = $"{char.ToUpper(contrato.metodo_pago[0]) + contrato.metodo_pago.Substring(1)} / {char.ToUpper(contrato.modalidad[0]) + contrato.modalidad.Substring(1)}";

                        if (DateTime.TryParse(contrato.fecha_venta, out DateTime fVenta))
                            LblFechaVenta.Text = fVenta.ToString("dd/MM/yyyy");

                        if (DateTime.TryParse(contrato.fecha_primer_pago, out DateTime fPrimerPago))
                            LblFechaPrimerPago.Text = fPrimerPago.ToString("dd/MM/yyyy");

                        if (contrato.tipo_pago.ToLower() == "credito")
                        {
                            PanelInfoCredito.Visibility = Visibility.Visible;
                            LblFrecuencia.Text = !string.IsNullOrEmpty(contrato.frecuencia_pago) ? char.ToUpper(contrato.frecuencia_pago[0]) + contrato.frecuencia_pago.Substring(1) : "-";
                            LblMontoPorCuota.Text = $"${contrato.monto_por_pago:N2}";
                        }
                        else
                        {
                            PanelInfoCredito.Visibility = Visibility.Collapsed;
                        }

                        // Resumen Financiero
                        LblSubtotalPaquete.Text = $"${contrato.precio_paquete_aplicado:N2}";
                        LblServiciosExtras.Text = $"${contrato.precio_servicios_aplicado:N2}";
                        LblMontoFinal.Text = $"${contrato.monto_final:N2}";
                        LblResumenEnganche.Text = $"${contrato.enganche:N2}";

                        decimal saldoPendiente = Math.Max(0, contrato.monto_final - contrato.enganche); // Ajustable si hay pagos acumulados
                        LblMontoRestante.Text = $"${saldoPendiente:N2}";
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
            this.NavigationService.Navigate(new EditarContratoPage(_contratoId));
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ContratosPage());
        }

        private void BreadcrumbContratos_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ContratosPage());
        }
    }

    public class ContratoDetalleModel
    {
        public int id { get; set; }
        public string folio { get; set; } = string.Empty;
        public int cliente_id { get; set; }
        public ClienteDetalleModel? cliente { get; set; }
        public PaqueteDetalleModel? paquete { get; set; }
        public UrnaDetalleModel? urna { get; set; }
        public string mascota_nombre { get; set; } = string.Empty;
        public string mascota_tipo { get; set; } = string.Empty;
        public string mascota_peso { get; set; } = string.Empty;
        public string mascota_edad { get; set; } = string.Empty;
        public decimal precio_paquete_aplicado { get; set; }
        public decimal precio_servicios_aplicado { get; set; }
        public decimal monto_final { get; set; }
        public string tipo_pago { get; set; } = string.Empty;
        public decimal enganche { get; set; }
        public string frecuencia_pago { get; set; } = string.Empty;
        public decimal monto_por_pago { get; set; }
        public string fecha_venta { get; set; } = string.Empty;
        public string fecha_primer_pago { get; set; } = string.Empty;
        public string metodo_pago { get; set; } = string.Empty;
        public string modalidad { get; set; } = string.Empty;
        public List<ServicioExtraItemDetalle> servicios_extras { get; set; } = new();
    }

    public class ClienteDetalleModel
    {
        public string nombre { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public string celular { get; set; } = string.Empty;
    }

    public class PaqueteDetalleModel
    {
        public string nombre { get; set; } = string.Empty;
    }

    public class UrnaDetalleModel
    {
        public string nombre { get; set; } = string.Empty;
    }

    public class ServicioExtraItemDetalle
    {
        public string nombre { get; set; } = string.Empty;
        public decimal precio_aplicado { get; set; }
        public string precio_aplicado_formato => $"${precio_aplicado:N2}";
    }
}