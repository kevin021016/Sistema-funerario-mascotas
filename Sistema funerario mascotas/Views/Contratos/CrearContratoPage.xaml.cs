using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Contratos
{
    public partial class CrearContratoPage : Page
    {
        private List<PaqueteModel> _paquetesList = new();
        private List<UrnaModel> _urnasList = new();
        private List<ServicioModel> _serviciosList = new();
        private ObservableCollection<ServicioExtraItem> _serviciosExtrasSeleccionados = new();

        public CrearContratoPage()
        {
            InitializeComponent();
            IcServiciosExtras.ItemsSource = _serviciosExtrasSeleccionados;

            // Asignar fecha actual en formato DD/MM/AAAA por defecto
            string fechaHoy = DateTime.Now.ToString("dd/MM/yyyy");
            TxtFechaVenta.Text = fechaHoy;
            TxtFechaPrimerPago.Text = fechaHoy;

            CargarCatalogosIniciales();
        }

        // Restringir entrada solo a números y barras diagonales
        private void Fecha_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        // Autoformato dinámico para DD/MM/AAAA
        private void TxtFechaVenta_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFormatoFecha(sender as Wpf.Ui.Controls.TextBox);
        }

        private void TxtFechaPrimerPago_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFormatoFecha(sender as Wpf.Ui.Controls.TextBox);
        }

        private void AplicarFormatoFecha(Wpf.Ui.Controls.TextBox textBox)
        {
            if (textBox == null) return;
            string text = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (text.Length > 8) text = text.Substring(0, 8);

            string formatted = "";
            if (text.Length > 0)
            {
                formatted = text.Substring(0, Math.Min(2, text.Length));
                if (text.Length >= 3)
                {
                    formatted += "/" + text.Substring(2, Math.Min(2, text.Length - 2));
                    if (text.Length >= 5)
                    {
                        formatted += "/" + text.Substring(4, Math.Min(4, text.Length - 4));
                    }
                }
            }

            if (textBox.Text != formatted)
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = formatted;
                textBox.CaretIndex = Math.Min(formatted.Length, textBox.Text.Length);
            }
        }

        private async void CargarCatalogosIniciales()
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // 1. Clientes con opción por defecto
                var resClientes = await client.GetAsync($"{ApiConfig.BaseUrl}clients/all");
                if (resClientes.IsSuccessStatusCode)
                {
                    var json = await resClientes.Content.ReadAsStringAsync();
                    var listaClientes = JsonSerializer.Deserialize<List<ClienteDropdownModel>>(json, opciones) ?? new List<ClienteDropdownModel>();

                    listaClientes.Insert(0, new ClienteDropdownModel { id = 0, nombre = "Seleccione un cliente", apellidos = "" });
                    CbClientes.ItemsSource = listaClientes;
                    CbClientes.SelectedIndex = 0;
                }

                // 2. Paquetes con opción por defecto
                var resPaquetes = await client.GetAsync($"{ApiConfig.BaseUrl}paquetes");
                if (resPaquetes.IsSuccessStatusCode)
                {
                    var json = await resPaquetes.Content.ReadAsStringAsync();
                    _paquetesList = ParsearListaGenerica<PaqueteModel>(json, opciones);

                    _paquetesList.Insert(0, new PaqueteModel { id = 0, nombre = "Seleccione un paquete", precio = 0 });
                    CbPaquetes.ItemsSource = _paquetesList;
                    CbPaquetes.SelectedIndex = 0;
                }

                // 3. Urnas con opción por defecto (Opcional, pero se añade por consistencia)
                var resUrnas = await client.GetAsync($"{ApiConfig.BaseUrl}urnas");
                if (resUrnas.IsSuccessStatusCode)
                {
                    var json = await resUrnas.Content.ReadAsStringAsync();
                    _urnasList = ParsearListaGenerica<UrnaModel>(json, opciones);

                    _urnasList.Insert(0, new UrnaModel { id = 0, nombre = "Ninguna / Seleccione urna" });
                    CbUrnas.ItemsSource = _urnasList;
                    CbUrnas.SelectedIndex = 0;
                }

                // 4. Servicios extras con opción por defecto
                var resServicios = await client.GetAsync($"{ApiConfig.BaseUrl}servicios");
                if (resServicios.IsSuccessStatusCode)
                {
                    var json = await resServicios.Content.ReadAsStringAsync();
                    _serviciosList = ParsearListaGenerica<ServicioModel>(json, opciones);

                    _serviciosList.Insert(0, new ServicioModel { id = 0, nombre = "Seleccione servicio extra", precio = 0 });
                    CbServiciosDisponibles.ItemsSource = _serviciosList;
                    CbServiciosDisponibles.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando catálogos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<T> ParsearListaGenerica<T>(string json, JsonSerializerOptions opciones)
        {
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json, opciones) ?? new List<T>();
            }
            catch
            {
                try
                {
                    var paginado = JsonSerializer.Deserialize<GenericPaginationWrapper<T>>(json, opciones);
                    return paginado?.data ?? new List<T>();
                }
                catch
                {
                    return new List<T>();
                }
            }
        }

        private void CbPaquetes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbPaquetes.SelectedItem is PaqueteModel paqueteSeleccionado && TxtPrecioPaquete != null)
            {
                if (paqueteSeleccionado.id > 0)
                {
                    TxtPrecioPaquete.Text = paqueteSeleccionado.precio.ToString("F2");
                }
                else
                {
                    TxtPrecioPaquete.Text = "0.00";
                }
            }
            RecalcularResumenFinanciero();
        }

        private void CbTipoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelCredito != null && PanelDesgloseCredito != null && CbTipoPago.SelectedItem is ComboBoxItem item)
            {
                bool esCredito = item.Content.ToString() == "Crédito";
                PanelCredito.Visibility = esCredito ? Visibility.Visible : Visibility.Collapsed;
                PanelDesgloseCredito.Visibility = esCredito ? Visibility.Visible : Visibility.Collapsed;
            }
            RecalcularResumenFinanciero();
        }

        private void CalculosFinancieros_Changed(object sender, TextChangedEventArgs e)
        {
            RecalcularResumenFinanciero();
        }

        private void AgregarServicioExtra_Click(object sender, RoutedEventArgs e)
        {
            if (CbServiciosDisponibles.SelectedItem is ServicioModel servicio && servicio.id > 0)
            {
                if (_serviciosExtrasSeleccionados.Any(s => s.servicio_id == servicio.id)) return;

                _serviciosExtrasSeleccionados.Add(new ServicioExtraItem
                {
                    servicio_id = servicio.id,
                    nombre = servicio.nombre,
                    precio_aplicado = servicio.precio,
                    cantidad = 1
                });
                RecalcularResumenFinanciero();
            }
        }

        private void QuiterServicioExtra_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int servicioId)
            {
                var itemAEliminar = _serviciosExtrasSeleccionados.FirstOrDefault(s => s.servicio_id == servicioId);
                if (itemAEliminar != null)
                {
                    _serviciosExtrasSeleccionados.Remove(itemAEliminar);
                    RecalcularResumenFinanciero();
                }
            }
        }

        private void RecalcularResumenFinanciero()
        {
            if (TxtPrecioPaquete == null || TxtEnganche == null || LblSubtotalPaquete == null || LblMontoFinal == null)
                return;

            decimal.TryParse(TxtPrecioPaquete.Text, out decimal precioPaquete);
            decimal totalExtras = _serviciosExtrasSeleccionados.Sum(s => s.precio_aplicado * s.cantidad);
            decimal montoFinal = precioPaquete + totalExtras;

            decimal.TryParse(TxtEnganche.Text, out decimal enganche);
            decimal restante = Math.Max(0, montoFinal - enganche);

            LblSubtotalPaquete.Text = $"${precioPaquete:N2}";
            LblServiciosExtras.Text = $"${totalExtras:N2}";
            LblMontoFinal.Text = $"${montoFinal:N2}";
            LblResumenEnganche.Text = $"${enganche:N2}";
            LblMontoRestante.Text = $"${restante:N2}";

            if (CbTipoPago.SelectedItem is ComboBoxItem item && item.Content.ToString() == "Crédito")
            {
                decimal.TryParse(TxtMontoPorPago.Text, out decimal montoPorPago);
                if (montoPorPago > 0 && restante > 0)
                {
                    int totalPagos = (int)Math.Ceiling(restante / montoPorPago);
                    LblPagosTotales.Text = totalPagos.ToString();
                    LblValorCuota.Text = $"${montoPorPago:N2}";
                }
                else
                {
                    LblPagosTotales.Text = "0";
                    LblValorCuota.Text = "$0.00";
                }
            }
        }

        private async void GuardarContrato_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ocultar errores previos
                TxtErrorCliente.Visibility = Visibility.Collapsed;
                TxtErrorMascotaNombre.Visibility = Visibility.Collapsed;
                TxtErrorMascotaTipo.Visibility = Visibility.Collapsed;
                TxtErrorPaquete.Visibility = Visibility.Collapsed;
                TxtErrorPrecioPaquete.Visibility = Visibility.Collapsed;
                TxtErrorMontoPorPago.Visibility = Visibility.Collapsed;
                TxtErrorFechaVenta.Visibility = Visibility.Collapsed;
                TxtErrorFechaPrimerPago.Visibility = Visibility.Collapsed;

                bool hayErrores = false;

                // Validación de cliente seleccionado (id == 0 es la opción por defecto)
                if (CbClientes.SelectedValue == null || (int)CbClientes.SelectedValue == 0)
                {
                    TxtErrorCliente.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (string.IsNullOrWhiteSpace(TxtMascotaNombre.Text))
                {
                    TxtErrorMascotaNombre.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (string.IsNullOrWhiteSpace(TxtMascotaTipo.Text))
                {
                    TxtErrorMascotaTipo.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                // Validación de paquete seleccionado (id == 0 es la opción por defecto)
                if (CbPaquetes.SelectedValue == null || (int)CbPaquetes.SelectedValue == 0)
                {
                    TxtErrorPaquete.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (!decimal.TryParse(TxtPrecioPaquete.Text, out decimal precioPaquete) || precioPaquete <= 0)
                {
                    TxtErrorPrecioPaquete.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                bool esCredito = (CbTipoPago.SelectedItem is ComboBoxItem item && item.Content.ToString() == "Crédito");

                if (esCredito)
                {
                    if (!decimal.TryParse(TxtMontoPorPago.Text, out decimal montoPorPagoValidacion) || montoPorPagoValidacion <= 0)
                    {
                        TxtErrorMontoPorPago.Visibility = Visibility.Visible;
                        hayErrores = true;
                    }
                }

                if (!DateTime.TryParseExact(TxtFechaVenta.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaVentaParsed))
                {
                    TxtErrorFechaVenta.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (!DateTime.TryParseExact(TxtFechaPrimerPago.Text.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaPrimerPagoParsed))
                {
                    TxtErrorFechaPrimerPago.Visibility = Visibility.Visible;
                    hayErrores = true;
                }

                if (hayErrores) return;

                decimal.TryParse(TxtEnganche.Text, out decimal enganche);
                decimal.TryParse(TxtMontoPorPago.Text, out decimal montoPorPago);
                decimal totalExtras = _serviciosExtrasSeleccionados.Sum(s => s.precio_aplicado * s.cantidad);

                int? urnaIdValido = (CbUrnas.SelectedValue != null && (int)CbUrnas.SelectedValue > 0) ? (int?)CbUrnas.SelectedValue : null;

                var payload = new
                {
                    cliente_id = (int)CbClientes.SelectedValue,
                    paquete_id = (int)CbPaquetes.SelectedValue,
                    urna_id = urnaIdValido,
                    mascota_nombre = TxtMascotaNombre.Text.Trim(),
                    mascota_tipo = TxtMascotaTipo.Text.Trim(),
                    mascota_peso = string.IsNullOrWhiteSpace(TxtMascotaPeso.Text) ? null : TxtMascotaPeso.Text.Trim(),
                    mascota_edad = string.IsNullOrWhiteSpace(TxtMascotaEdad.Text) ? null : TxtMascotaEdad.Text.Trim(),
                    precio_paquete_aplicado = precioPaquete,
                    precio_servicios_aplicado = totalExtras,
                    monto_final = precioPaquete + totalExtras,
                    tipo_pago = esCredito ? "credito" : "contado",
                    enganche = enganche,
                    frecuencia_pago = esCredito && CbFrecuencia.SelectedItem is ComboBoxItem freq ? freq.Content.ToString().ToLower() : null,
                    monto_por_pago = esCredito ? montoPorPago : 0,
                    fecha_venta = fechaVentaParsed.ToString("yyyy-MM-dd"),
                    fecha_primer_pago = fechaPrimerPagoParsed.ToString("yyyy-MM-dd"),
                    metodo_pago = CbMetodoPago.SelectedItem is ComboBoxItem met ? met.Content.ToString().ToLower() : "efectivo",
                    modalidad = CbModalidad.SelectedItem is ComboBoxItem mod ? mod.Content.ToString().ToLower() : "oficina",
                    servicios_extras = _serviciosExtrasSeleccionados.Select(s => new {
                        servicio_id = s.servicio_id,
                        precio_aplicado = s.precio_aplicado,
                        cantidad = s.cantidad
                    }).ToList()
                };

                var client = new HttpClient();
                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiConfig.BaseUrl}contratos")
                {
                    Content = jsonContent
                };
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    MostrarSnackbar("Éxito", "Contrato registrado correctamente.", true);
                    await System.Threading.Tasks.Task.Delay(1000);
                    this.NavigationService.Navigate(new ContratosPage());
                }
                else
                {
                    MostrarSnackbar("Error", "No se pudo registrar el contrato.", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BreadcrumbContratos_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ContratosPage());
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

    public class GenericPaginationWrapper<T>
    {
        public List<T> data { get; set; } = new();
    }

    public class ClienteDropdownModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public string nombre_completo => id == 0 ? nombre : $"{nombre} {apellidos}";
    }

    public class PaqueteModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public decimal precio { get; set; }
    }

    public class UrnaModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
    }

    public class ServicioModel
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public decimal precio { get; set; }
    }

    public class ServicioExtraItem
    {
        public int servicio_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public decimal precio_aplicado { get; set; }
        public int cantidad { get; set; }
        public string precio_aplicado_formato => $"${precio_aplicado:N2}";
    }
}