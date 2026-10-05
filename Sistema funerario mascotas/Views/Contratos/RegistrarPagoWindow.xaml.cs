using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace Sistema_funerario_mascotas.Views.Contratos
{
    public partial class RegistrarPagoWindow : Window
    {
        private int _contratoId;

        public RegistrarPagoWindow(int contratoId)
        {
            InitializeComponent();
            _contratoId = contratoId;
        }

        private async void GuardarPago_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a 0.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                string tipoPago = "abono";
                if (CmbTipoPago.SelectedItem is ComboBoxItem itemTipo && itemTipo.Content != null)
                {
                    tipoPago = itemTipo.Content.ToString();
                }

                string metodoPago = "Efectivo";
                if (CmbMetodo.SelectedItem is ComboBoxItem itemMetodo && itemMetodo.Content != null)
                {
                    metodoPago = itemMetodo.Content.ToString();
                }

                var datosPago = new Dictionary<string, string>
                {
                    { "contrato_id", _contratoId.ToString() },
                    { "monto", monto.ToString() },
                    { "tipo_pago", tipoPago },
                    { "metodo_pago", metodoPago },
                    { "fecha_pago", DateTime.Now.ToString("yyyy-MM-dd") },
                    { "referencia", TxtReferencia.Text.Trim() },
                    { "observaciones", TxtObservaciones.Text.Trim() }
                };

                var content = new FormUrlEncodedContent(datosPago);
                var response = await client.PostAsync($"{ApiConfig.BaseUrl}pagos", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Pago registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    var errJson = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error al registrar el pago: {errJson}", "Error del Servidor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Excepción", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}