using Sistema_funerario_mascotas.Views;
using Sistema_funerario_mascotas.Views.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Sistema_funerario_mascotas
{
    public partial class UsuariosPage : Page
    {
        private int _paginaActual = 1;
        private int _ultimaPagina = 1;

        public UsuariosPage()
        {
            InitializeComponent();
            CargarUsuariosPagina(_paginaActual);
        }

        private async void CargarUsuariosPagina(int pagina)
        {
            try
            {
                string filtro = TxtBuscador != null ? TxtBuscador.Text.Trim() : string.Empty;

                var client = new HttpClient();
                var url = $"http://127.0.0.1:8000/api/users?page={pagina}&search={Uri.EscapeDataString(filtro)}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var paginatedResponse = JsonSerializer.Deserialize<PaginatedUsersResponse>(jsonString, opciones);

                    if (paginatedResponse != null)
                    {
                        IcUsuarios.ItemsSource = paginatedResponse.data;
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
            CargarUsuariosPagina(_paginaActual);
        }

        private async void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                CargarUsuariosPagina(_paginaActual);
            }
        }

        private async void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _ultimaPagina)
            {
                _paginaActual++;
                CargarUsuariosPagina(_paginaActual);
            }
        }

        // Muestra una notificación tipo Snackbar (éxito o error)
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
            if (sender is Button button && button.CommandParameter is int usuarioId)
            {
                var confirmWindow = new ConfirmDialogWindow("Confirmar Eliminación", "¿Estás seguro de que deseas eliminar este usuario?")
                {
                    Owner = Application.Current.MainWindow
                };

                confirmWindow.ShowDialog();

                if (confirmWindow.Confirmed)
                {
                    try
                    {
                        var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Delete, $"http://127.0.0.1:8000/api/users/{usuarioId}");
                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                        var response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            MostrarSnackbar("Error", "No se pudo eliminar el usuario.", exito: false);
                            return;
                        }

                        int miUsuarioId = int.Parse(SessionManager.UserId);

                        if (usuarioId == miUsuarioId)
                        {
                            MessageBox.Show("Has eliminado tu propia cuenta de usuario. El programa se cerrará.", "Sesión Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            MostrarSnackbar("Éxito", "Usuario eliminado correctamente.", exito: true);
                            CargarUsuariosPagina(_paginaActual);
                        }
                    }
                    catch (Exception ex)
                    {
                        MostrarSnackbar("Error", $"Error de conexión: {ex.Message}", exito: false);
                    }
                }
            }
        }

        private void IrAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new CrearUsuarioPage());
        }

        private void IrEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UsuarioModel usuario)
            {
                this.NavigationService.Navigate(new Views.Users.EditarUsuarioPage(usuario));
            }
        }

        private void IrDetallesUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UsuarioModel usuario)
            {
                this.NavigationService.Navigate(new Views.Users.DetallesUsuarioPage(usuario));
            }
        }

        // EXPORTAR A EXCEL (Generando archivo CSV compatible con Excel)
        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var usuarios = await ObtenerTodosLosUsuariosParaReporte();
                if (usuarios == null || usuarios.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivo CSV (*.csv)|*.csv",
                    FileName = $"Reporte_Usuarios_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("ID,Nombre,Apellidos,Rol,Email,Teléfono,Última Conexión");

                    foreach (var u in usuarios)
                    {
                        sb.AppendLine($"{u.id},\"{u.name}\",\"{u.apellidos}\",\"{u.rol}\",\"{u.email}\",\"{u.telefono}\",\"{u.last_active_at}\"");
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

        // EXPORTAR A PDF CON PDFSHARP
        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var usuarios = await ObtenerTodosLosUsuariosParaReporte();
                if (usuarios == null || usuarios.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivo PDF (*.pdf)|*.pdf",
                    FileName = $"Reporte_Usuarios_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
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

                    // Dibujar Título
                    gfx.DrawString("Reporte de Usuarios - Sistema Funerario", fontTitulo, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(40, yPoint));
                    yPoint += 20;
                    gfx.DrawString($"Fecha de emisión: {DateTime.Now}", fontSub, PdfSharp.Drawing.XBrushes.Gray, new PdfSharp.Drawing.XPoint(40, yPoint));
                    yPoint += 35;

                    // Dibujar Cabecera de la Tabla
                    gfx.DrawString("Nombre", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(40, yPoint));
                    gfx.DrawString("Apellidos", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(150, yPoint));
                    gfx.DrawString("Rol", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(280, yPoint));
                    gfx.DrawString("Email", fontHeader, PdfSharp.Drawing.XBrushes.DarkBlue, new PdfSharp.Drawing.XPoint(360, yPoint));

                    yPoint += 15;
                    gfx.DrawLine(PdfSharp.Drawing.XPens.LightGray, 40, yPoint, 550, yPoint);
                    yPoint += 10;

                    // Dibujar Filas de Datos
                    foreach (var u in usuarios)
                    {
                        if (yPoint > page.Height.Point - 50)
                        {
                            page = document.AddPage();
                            gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                            yPoint = 40;
                        }

                        gfx.DrawString(u.name ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(40, yPoint));
                        gfx.DrawString(u.apellidos ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(150, yPoint));
                        gfx.DrawString(u.rol ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(280, yPoint));
                        gfx.DrawString(u.email ?? "", fontBody, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(360, yPoint));

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

        // Método auxiliar para pedir todos los usuarios al backend sin paginación para los reportes
        private async System.Threading.Tasks.Task<List<UsuarioModel>> ObtenerTodosLosUsuariosParaReporte()
        {
            try
            {
                var client = new HttpClient();
                // Usamos la ruta principal sin paginación si tu API lo soporta, 
                // o consultamos la misma ruta actual asegurando cabeceras
                var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:8000/api/users/all");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var lista = JsonSerializer.Deserialize<List<UsuarioModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (lista != null && lista.Count > 0) return lista;
                }
            }
            catch { }

            // Plan B: Si la ruta de todos falla, extraemos los usuarios que el usuario ya tiene visibles en pantalla
            if (IcUsuarios.ItemsSource is List<UsuarioModel> usuariosVisibles)
            {
                return usuariosVisibles;
            }

            return null;
        }
    }

    public class PaginatedUsersResponse
    {
        public int current_page { get; set; }
        public List<UsuarioModel> data { get; set; }
        public int last_page { get; set; }
    }

    public class UsuarioModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string apellidos { get; set; }
        public string rol { get; set; }
        public string email { get; set; }
        public string telefono { get; set; }
        public string last_active_at { get; set; }
    }
}