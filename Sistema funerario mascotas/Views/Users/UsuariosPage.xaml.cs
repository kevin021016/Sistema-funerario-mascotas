using Sistema_funerario_mascotas.Views;
using Sistema_funerario_mascotas.Views.Users;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Sistema_funerario_mascotas
{
    public partial class UsuariosPage : Page
    {
        public UsuariosPage()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private async void CargarUsuarios()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:8000/api/users");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    // Deserializamos el JSON de Laravel a una lista de usuarios
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioModel>>(jsonString, opciones);

                    // Vinculamos los datos a la interfaz gráfica
                    IcUsuarios.ItemsSource = usuarios;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con la API: {ex.Message}", "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            // Obtenemos el ID del usuario desde el CommandParameter del botón presionado
            if (sender is Button button && button.CommandParameter is int usuarioId)
            {
                // Mostramos nuestra ventana de confirmación personalizada
                var confirmWindow = new ConfirmDialogWindow("Confirmar Eliminación", "¿Estás seguro de que deseas eliminar este usuario?")
                {
                    Owner = Application.Current.MainWindow
                };

                confirmWindow.ShowDialog();

                // Si el usuario presionó "Sí"
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

                        // Si el ID eliminado coincide con el mío, cerramos el programa
                        if (usuarioId == miUsuarioId)
                        {
                            MessageBox.Show("Has eliminado tu propia cuenta de usuario. El programa se cerrará.", "Sesión Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Cierra la aplicación WPF por completo
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            // Si borramos a otro usuario, simplemente recargamos la lista
                            CargarUsuarios();
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
            // El DataContext del botón está ligado al usuario correspondiente en el ItemsControl
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

    // Modelo auxiliar para mapear los campos que devuelve Laravel
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