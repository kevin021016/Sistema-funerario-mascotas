using Sistema_funerario_mascotas.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Contratos
{
    public partial class ContratosPage : Page
    {
        private int _paginaActual = 1;
        private int _ultimaPagina = 1;

        public ContratosPage()
        {
            InitializeComponent();
            CargarContratosPagina(_paginaActual);
        }

        private void IrAgregarContrato_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new CrearContratoPage());
        }

        private void IrEditarContrato_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ContratoModel contratoSeleccionado)
            {
                // Pasamos solo el id al page de edición; la página consultará la API para cargar datos completos
                this.NavigationService.Navigate(new EditarContratoPage(contratoSeleccionado.id));
            }
        }

        private void IrDetallesContrato_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ContratoModel contratoSeleccionado)
            {
                this.NavigationService.Navigate(new DetallesContratoPage(contratoSeleccionado.id));
            }
        }

        private async void CargarContratosPagina(int pagina)
        {
            try
            {
                string filtro = TxtBuscador != null ? TxtBuscador.Text.Trim() : string.Empty;

                var client = new HttpClient();
                var url = $"{ApiConfig.BaseUrl}contratos?page={pagina}&search={Uri.EscapeDataString(filtro)}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var paginatedResponse = JsonSerializer.Deserialize<PaginatedContratosResponse>(jsonString, opciones);

                    if (paginatedResponse != null)
                    {
                        IcContratos.ItemsSource = paginatedResponse.data;
                        _paginaActual = paginatedResponse.current_page;
                        _ultimaPagina = paginatedResponse.last_page;

                        TxtPaginaActual.Text = $"Página {_paginaActual} de {_ultimaPagina}";

                        BtnAnterior.IsEnabled = _paginaActual > 1;
                        BtnSiguiente.IsEnabled = _paginaActual < _ultimaPagina;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con la API: {ex.Message}", "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            _paginaActual = 1;
            CargarContratosPagina(_paginaActual);
        }

        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                CargarContratosPagina(_paginaActual);
            }
        }

        private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _ultimaPagina)
            {
                _paginaActual++;
                CargarContratosPagina(_paginaActual);
            }
        }

        private async void MostrarSnackbar(string titulo, string mensaje, bool exito)
        {
            var snackbar = new Wpf.Ui.Controls.Snackbar(SnackbarHost)
            {
                Title = titulo,
                Content = mensaje,
                Appearance = exito ? Wpf.Ui.Controls.ControlAppearance.Dark : Wpf.Ui.Controls.ControlAppearance.Danger,
                Icon = new Wpf.Ui.Controls.SymbolIcon
                {
                    Symbol = exito ? Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24 : Wpf.Ui.Controls.SymbolRegular.DismissCircle24
                },
                Timeout = TimeSpan.FromSeconds(4)
            };

            await snackbar.ShowAsync();
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int contratoId)
            {
                var confirmWindow = new ConfirmDialogWindow("Confirmar Eliminación", "¿Estás seguro de que deseas eliminar este contrato?")
                {
                    Owner = Application.Current.MainWindow
                };

                confirmWindow.ShowDialog();

                if (confirmWindow.Confirmed)
                {
                    try
                    {
                        var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}contratos/{contratoId}");
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                        var response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            MostrarSnackbar("Error", "No se pudo eliminar el contrato.", exito: false);
                            return;
                        }

                        MostrarSnackbar("Éxito", "Contrato eliminado correctamente.", exito: true);
                        CargarContratosPagina(_paginaActual);
                    }
                    catch (Exception ex)
                    {
                        MostrarSnackbar("Error", $"Error de conexión: {ex.Message}", exito: false);
                    }
                }
            }
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contratos = await ObtenerTodosLosContratosParaReporte();
                if (contratos == null || contratos.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivo CSV (*.csv)|*.csv",
                    FileName = $"Reporte_Contratos_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("ID,Folio,Cliente,Mascota,Monto Final,Estatus,Fecha Venta");

                    foreach (var c in contratos)
                    {
                        sb.AppendLine($"{c.id},\"{c.folio}\",\"{c.cliente_nombre_completo}\",\"{c.mascota_nombre}\",{c.monto_final},\"{c.estatus}\",\"{c.fecha_venta}\"");
                    }

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Reporte exportado a Excel exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar a Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contratos = await ObtenerTodosLosContratosParaReporte();
                if (contratos == null || contratos.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivo PDF (*.pdf)|*.pdf",
                    FileName = $"Reporte_Contratos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;

                    PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                    PdfSharp.Pdf.PdfPage page = document.AddPage();
                    PdfSharp.Drawing.XGraphics gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

                    PdfSharp.Drawing.XFont fontTitulo = new PdfSharp.Drawing.XFont("Arial", 16, PdfSharp.Drawing.XFontStyleEx.Bold);
                    PdfSharp.Drawing.XFont fontSub = new PdfSharp.Drawing.XFont("Arial", 10, PdfSharp.Drawing.XFontStyleEx.Italic);
                    PdfSharp.Drawing.XFont fontHeader = new PdfSharp.Drawing.XFont("Arial", 10, PdfSharp.Drawing.XFontStyleEx.Bold);
                    PdfSharp.Drawing.XFont fontBody = new PdfSharp.Drawing.XFont("Arial", 9, PdfSharp.Drawing.XFontStyleEx.Regular);

                    double yPoint = 40;

                    gfx.DrawString("Reporte de Contratos - Sistema Funerario", fontTitulo, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(40, yPoint));
                    yPoint += 20;
                    gfx.DrawString($"Fecha de emisión: {DateTime.Now}", fontSub, PdfSharp.Drawing.XBrushes.Gray, new PdfSharp.Drawing.XPoint(40, yPoint));
                    yPoint += 35;

                    gfx.DrawString("Folio", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(40, yPoint));
                    gfx.DrawString("Cliente", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(140, yPoint));
                    gfx.DrawString("Mascota", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(280, yPoint));
                    gfx.DrawString("Total", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(390, yPoint));
                    gfx.DrawString("Estatus", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(470, yPoint));

                    yPoint += 15;
                    gfx.DrawLine(PdfSharp.Drawing.XPens.LightGray, 40, yPoint, 550, yPoint);
                    yPoint += 10;

                    foreach (var c in contratos)
                    {
                        if (yPoint > page.Height.Point - 50)
                        {
                            page = document.AddPage();
                            gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                            yPoint = 40;
                        }

                        gfx.DrawString(c.folio ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(40, yPoint));
                        gfx.DrawString(c.cliente_nombre_completo ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(140, yPoint));
                        gfx.DrawString(c.mascota_nombre ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(280, yPoint));
                        gfx.DrawString($"${c.monto_final:N2}", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(390, yPoint));
                        gfx.DrawString(c.estatus ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(470, yPoint));

                        yPoint += 20;
                    }

                    document.Save(saveFileDialog.FileName);
                    MessageBox.Show("PDF generado y guardado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<List<ContratoModel>> ObtenerTodosLosContratosParaReporte()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}contratos/all");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var lista = JsonSerializer.Deserialize<List<ContratoModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (lista != null && lista.Count > 0) return lista;
                }
            }
            catch { }

            if (IcContratos.ItemsSource is List<ContratoModel> contratosVisibles)
            {
                return contratosVisibles;
            }

            return null;
        }

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(DashboardPage));
            }
        }
    }

    public class PaginatedContratosResponse
    {
        public int current_page { get; set; }
        public List<ContratoModel> data { get; set; }
        public int last_page { get; set; }
    }

    public class ContratoModel
    {
        public int id { get; set; }
        public string folio { get; set; }
        public int cliente_id { get; set; }
        public ClienteAnidado cliente { get; set; }
        public string mascota_nombre { get; set; }
        public decimal monto_final { get; set; }
        public string estatus { get; set; }
        public string fecha_venta { get; set; }

        public string cliente_nombre_completo => cliente != null ? $"{cliente.nombre} {cliente.apellidos}" : "N/A";
        public string monto_final_formato => $"${monto_final:N2}";
    }

    public class ClienteAnidado
    {
        public string nombre { get; set; }
        public string apellidos { get; set; }
    }
}