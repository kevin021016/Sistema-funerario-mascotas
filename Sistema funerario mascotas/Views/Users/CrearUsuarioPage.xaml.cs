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

        // Oculta y limpia todos los textos de error previos
        private void LimpiarErroresUI()
        {
            TxtErrorNombre.Visibility = Visibility.Hidden;
            TxtErrorApellidos.Visibility = Visibility.Hidden;
            TxtErrorEmail.Visibility = Visibility.Hidden;
            TxtErrorTelefono.Visibility = Visibility.Hidden;
            TxtErrorRol.Visibility = Visibility.Hidden;
            TxtErrorPassword.Visibility = Visibility.Hidden;
            TxtErrorPasswordConfirm.Visibility = Visibility.Hidden;
        }

        // Expresión regular sencilla para validar el formato del correo electrónico
        private bool EsCorreoValido(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarErroresUI();
            bool hayErrores = false;

            // 1. Validar Nombre
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                TxtErrorNombre.Text = "El campo nombre es obligatorio.";
                TxtErrorNombre.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            // 2. Validar Correo
            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                TxtErrorEmail.Text = "El correo electrónico es obligatorio.";
                TxtErrorEmail.Visibility = Visibility.Visible;
                hayErrores = true;
            }
            else if (!EsCorreoValido(TxtEmail.Text.Trim()))
            {
                TxtErrorEmail.Text = "Ingrese una dirección de correo válida.";
                TxtErrorEmail.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            // 3. Validar Rol (Asegurar que no esté en la opción por defecto)
            if (CmbRol.SelectedIndex <= 0)
            {
                TxtErrorRol.Text = "Debe seleccionar un rol válido.";
                TxtErrorRol.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            // 4. Validar Contraseña (mínimo 8 caracteres)
            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                TxtErrorPassword.Text = "La contraseña es obligatoria.";
                TxtErrorPassword.Visibility = Visibility.Visible;
                hayErrores = true;
            }
            else if (TxtPassword.Password.Length < 8)
            {
                TxtErrorPassword.Text = "La contraseña debe tener al menos 8 caracteres.";
                TxtErrorPassword.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            // 5. Validar Confirmación de Contraseña
            if (TxtPassword.Password != TxtPasswordConfirm.Password)
            {
                TxtErrorPasswordConfirm.Text = "Las contraseñas no coinciden.";
                TxtErrorPasswordConfirm.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            // Si hay errores locales, detenemos el proceso y mostramos la alerta general
            if (hayErrores)
            {
                await MostrarAlerta("Error de validación", "Uno o más campos son incorrectos.");
                return;
            }

            string rolSeleccionado = ((ComboBoxItem)CmbRol.SelectedItem).Content.ToString();

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

                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {SessionManager.AuthToken}");

                var jsonContent = JsonSerializer.Serialize(nuevoUsuario);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await MostrarAlerta("Éxito", "¡Usuario creado exitosamente!");
                    BtnCancelar_Click(null, null); // Limpiar formulario
                }
                else
                {
                    // Si el servidor aún rechaza algo (ej. correo ya registrado en BD), lo manejamos de forma limpia
                    var errorResponse = await response.Content.ReadAsStringAsync();

                    if (errorResponse.Contains("email"))
                    {
                        TxtErrorEmail.Text = "Este correo electrónico ya está registrado.";
                        TxtErrorEmail.Visibility = Visibility.Visible;
                    }

                    await MostrarAlerta("Error de servidor", "No se pudo guardar el usuario. Verifique los datos.");
                }
            }
            catch (Exception ex)
            {
                await MostrarAlerta("Excepción", $"Error de conexión: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task MostrarAlerta(string titulo, string mensaje)
        {
            var customDialog = new CustomMessageWindow(titulo, mensaje)
            {
                Owner = Application.Current.MainWindow
            };

            customDialog.ShowDialog();
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                this.NavigationService.Navigate(new UsuariosPage());
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnVolver_Click(sender, e);
        }

        private void BreadcrumbUsuarios_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new UsuariosPage());
        }

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new DashboardPage());
        }
    }
}