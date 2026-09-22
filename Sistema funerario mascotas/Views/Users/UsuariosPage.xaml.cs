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

        private void IrAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para navegar a la vista de crear usuario
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