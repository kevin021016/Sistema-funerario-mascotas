using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Users
{
    public partial class CrearUsuarioPage : Page
    {
        public CrearUsuarioPage()
        {
            InitializeComponent();
        }

        private void Numero_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Texto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string textoPegado = (string)e.DataObject.GetData(DataFormats.Text);
                Regex regex = new Regex("[^0-9]+");
                if (regex.IsMatch(textoPegado))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos vacíos básicos
            if (string.IsNullOrWhiteSpace(TxtNombre.Text) ||
                string.IsNullOrWhiteSpace(TxtEmail.Text) ||
                string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                await MostrarAlerta("Campos requeridos", "Por favor, llena al menos el Nombre, Correo y Contraseña.");
                return;
            }

            // Validar contraseñas iguales
            if (TxtPassword.Password != TxtPasswordConfirm.Password)
            {
                await MostrarAlerta("Error de validación", "Las contraseñas no coinciden.");
                return;
            }

            // Obtener el rol seleccionado
            string rolSeleccionado = "Vendedor";
            if (CmbRol.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                rolSeleccionado = selectedItem.Content.ToString();
            }

            var nuevoUsuario = new
            {
                name = TxtNombre.Text.Trim(),
                apellidos = TxtApellidos.Text.Trim(),
                email = TxtEmail.Text.Trim(),
                rol = rolSeleccionado,
                telefono = TxtTelefono.Text.Trim(),
                password = TxtPassword.Password
            };

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8000/api/users");

                request.Headers.Add("Authorization", $"Bearer {SessionManager.AuthToken}");

                var jsonContent = JsonSerializer.Serialize(nuevoUsuario);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await MostrarAlerta("Éxito", "¡Usuario creado exitosamente!");

                    // Opcional: Limpiar campos o redirigir
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await MostrarAlerta("Error de API", $"No se pudo guardar el usuario: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                await MostrarAlerta("Excepción", $"Error de conexión: {ex.Message}");
            }
        }

        // Método auxiliar para mostrar los MessageBox de WPF UI de forma limpia
        private async System.Threading.Tasks.Task MostrarAlerta(string titulo, string mensaje)
        {
            var customDialog = new CustomMessageWindow(titulo, mensaje)
            {
                Owner = Application.Current.MainWindow
            };

            customDialog.ShowDialog();
            await System.Threading.Tasks.Task.CompletedTask;
        }
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cancelar o volver atrás
        }
    }
}