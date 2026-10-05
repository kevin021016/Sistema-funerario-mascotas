using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Contratos
{
    public partial class DetallesContratoPage : Page
    {
        private int _contratoId;

        public DetallesContratoPage(int contratoId)
        {
            InitializeComponent();
            _contratoId = contratoId;
            CargarDetallesContrato();
        }

        private async void CargarDetallesContrato()
        {
            try
            {
                PanelLoader.Visibility = Visibility.Visible;
                GridContenidoPrincipal.Visibility = Visibility.Collapsed;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.GetAsync($"{ApiConfig.BaseUrl}contratos/{_contratoId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Configuramos el lector para aceptar números enviados como strings desde Railway
                    var opciones = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    var contrato = JsonSerializer.Deserialize<ContratoDetalleModel>(json, opciones);

                    if (contrato != null)
                    {
                        TxtTituloFolio.Text = $"Detalles del Contrato: {contrato.folio}";

                        // 1. Cliente y Mascota
                        LblClienteNombre.Text = contrato.cliente?.nombre_completo ?? "N/A";
                        LblClienteTelefono.Text = contrato.cliente?.telefono ?? "N/A";
                        LblMascotaNombre.Text = contrato.mascota_nombre ?? "N/A";
                        LblMascotaTipo.Text = contrato.mascota_tipo ?? "N/A";
                        LblMascotaPeso.Text = string.IsNullOrWhiteSpace(contrato.mascota_peso) ? "No especificado" : $"{contrato.mascota_peso} kg";
                        LblMascotaEdad.Text = string.IsNullOrWhiteSpace(contrato.mascota_edad) ? "No especificada" : contrato.mascota_edad;

                        // 2. Paquete y Urna
                        LblPaqueteNombre.Text = contrato.paquete?.nombre ?? "Paquete personalizado / N/A";
                        LblPaquetePrecio.Text = $"${contrato.precio_paquete_aplicado:N2}";
                        LblUrnaNombre.Text = contrato.urna != null ? $"{contrato.urna.nombre} ({contrato.urna.codigo})" : "Ninguna asignada";

                        // 3. Servicios Extras
                        if (contrato.servicios_extras != null && contrato.servicios_extras.Count > 0)
                        {
                            IcServiciosExtrasDetalle.ItemsSource = contrato.servicios_extras;
                            LblSinExtras.Visibility = Visibility.Collapsed;
                            IcServiciosExtrasDetalle.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            IcServiciosExtrasDetalle.Visibility = Visibility.Collapsed;
                            LblSinExtras.Visibility = Visibility.Visible;
                        }

                        // 4. Esquema Financiero y Fechas
                        LblTipoPago.Text = contrato.tipo_pago;
                        LblMetodoModalidad.Text = contrato.metodo_pago;
                        LblFechaVenta.Text = contrato.fecha_venta;
                        LblFechaPrimerPago.Text = contrato.fecha_primer_pago;

                        if (contrato.tipo_pago?.ToLower() == "credito" || contrato.tipo_pago?.ToLower() == "crédito")
                        {
                            PanelInfoCredito.Visibility = Visibility.Visible;
                            LblFrecuencia.Text = contrato.frecuencia_pago ?? "-";
                            LblMontoPorCuota.Text = $"${contrato.monto_por_cuota:N2}";
                        }
                        else
                        {
                            PanelInfoCredito.Visibility = Visibility.Collapsed;
                        }

                        // Resumen Derecho
                        LblSubtotalPaquete.Text = $"${contrato.precio_paquete_aplicado:N2}";
                        LblServiciosExtras.Text = $"${contrato.precio_servicios_aplicado:N2}";
                        LblMontoFinal.Text = $"${contrato.monto_final:N2}";
                        LblResumenEnganche.Text = $"${contrato.enganche:N2}";
                        LblMontoRestante.Text = $"${contrato.monto_restante:N2}";

                        // 5. Historial de Pagos y Abonos
                        if (contrato.pagos != null && contrato.pagos.Count > 0)
                        {
                            IcPagosContrato.ItemsSource = contrato.pagos;
                            LblSinPagos.Visibility = Visibility.Collapsed;
                            IcPagosContrato.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            IcPagosContrato.Visibility = Visibility.Collapsed;
                            LblSinPagos.Visibility = Visibility.Visible;
                        }
                    }
                }

                PanelLoader.Visibility = Visibility.Collapsed;
                GridContenidoPrincipal.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                PanelLoader.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar el contrato: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VerObservacionesPago_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PagoModel pago)
            {
                string observaciones = string.IsNullOrWhiteSpace(pago.observaciones)
                    ? "Este pago no cuenta con observaciones registradas."
                    : pago.observaciones;

                MessageBox.Show(observaciones, "Observaciones del Pago", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void EliminarPago_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int pagoId)
            {
                var resultado = MessageBox.Show("¿Está seguro de eliminar este pago? El monto será devuelto al saldo restante del contrato.", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                        var response = await client.DeleteAsync($"{ApiConfig.BaseUrl}pagos/{pagoId}");
                        if (response.IsSuccessStatusCode)
                        {
                            MostrarSnackbarLocal("Éxito", "Pago eliminado correctamente y saldo actualizado.", true);
                            await System.Threading.Tasks.Task.Delay(1000);
                            CargarDetallesContrato(); // Recargar datos de la vista
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            MessageBox.Show($"No se pudo eliminar el pago: {errorContent}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a editar contrato si lo requieres
            // this.NavigationService.Navigate(new EditarContratoPage(_contratoId));
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ContratosPage());
        }

        private void BreadcrumbContratos_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ContratosPage());
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

        private void BtnRegistrarPago_Click(object sender, RoutedEventArgs e)
        {
            var ventanaPago = new RegistrarPagoWindow(_contratoId);
            ventanaPago.Owner = Window.GetWindow(this);

            if (ventanaPago.ShowDialog() == true)
            {
                CargarDetallesContrato(); // Recarga los datos del contrato y refresca la tabla de pagos automáticamente
            }
        }

        // Clases modelo de apoyo
        public class ContratoDetalleModel
        {
            public int id { get; set; }
            public string folio { get; set; } = string.Empty;
            public ClienteModel cliente { get; set; }
            public string mascota_nombre { get; set; } = string.Empty;
            public string mascota_tipo { get; set; } = string.Empty;
            public string mascota_peso { get; set; } = string.Empty;
            public string mascota_edad { get; set; } = string.Empty;
            public PaqueteModel paquete { get; set; }
            public UrnaReferenciaModel urna { get; set; }
            public List<ServicioExtraModel> servicios_extras { get; set; } = new();
            public List<PagoModel> pagos { get; set; } = new();
            public string tipo_pago { get; set; } = string.Empty;
            public string metodo_pago { get; set; } = string.Empty;
            public string fecha_venta { get; set; } = string.Empty;
            public string fecha_primer_pago { get; set; } = string.Empty;
            public string frecuencia_pago { get; set; } = string.Empty;
            public decimal monto_por_cuota { get; set; }
            public decimal precio_paquete_aplicado { get; set; }
            public decimal precio_servicios_aplicado { get; set; }
            public decimal monto_final { get; set; }
            public decimal enganche { get; set; }
            public decimal monto_restante { get; set; }
        }

        public class ClienteModel
        {
            public string nombre_completo { get; set; } = string.Empty;
            public string telefono { get; set; } = string.Empty;
        }

        public class PaqueteModel
        {
            public string nombre { get; set; } = string.Empty;
        }

        public class UrnaReferenciaModel
        {
            public string codigo { get; set; } = string.Empty;
            public string nombre { get; set; } = string.Empty;
        }

        public class ServicioExtraModel
        {
            public string nombre { get; set; } = string.Empty;
            public decimal precio { get; set; }
            public string precio_aplicado_formato => $"${precio:N2}";
        }

        public class PagoModel
        {
            public int id { get; set; }
            public decimal monto { get; set; }
            public string tipo_pago { get; set; } = string.Empty;
            public string metodo_pago { get; set; } = string.Empty;
            public string referencia { get; set; } = string.Empty;
            public string fecha_pago { get; set; } = string.Empty;
            public string observaciones { get; set; } = string.Empty;
            public string estatus { get; set; } = string.Empty;

            public string monto_formato => $"${monto:N2}";
        }
    }
}