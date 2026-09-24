using Sistema_funerario_mascotas.Views;
using Sistema_funerario_mascotas.Views.Users;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
                // Obtenemos el texto actual del buscador de forma segura
                string filtro = TxtBuscador != null ? TxtBuscador.Text.Trim() : string.Empty;

                var client = new HttpClient();
                // Adjuntamos el parámetro 'search' a la URL junto con la paginación
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

        // Evento que se dispara automáticamente al escribir en el buscador
        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            _paginaActual = 1; // Reiniciamos a la página 1 al buscar
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

                        int miUsuarioId = int.Parse(SessionManager.UserId);

                        if (usuarioId == miUsuarioId)
                        {
                            MessageBox.Show("Has eliminado tu propia cuenta de usuario. El programa se cerrará.", "Sesión Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            CargarUsuariosPagina(_paginaActual);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error de conexión: {ex.Message}", "Excepción", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ExportPdf_Click(object sender, RoutedEventArgs e) { }
        private void ExportExcel_Click(object sender, RoutedEventArgs e) { }
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