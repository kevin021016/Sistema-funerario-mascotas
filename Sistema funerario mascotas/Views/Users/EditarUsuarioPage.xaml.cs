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
    public partial class EditarUsuarioPage : Page
    {
        private Sistema_funerario_mascotas.UsuarioModel _usuario;

        public EditarUsuarioPage(Sistema_funerario_mascotas.UsuarioModel usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            CargarDatosEnFormulario();
        }

        private void CargarDatosEnFormulario()
        {
            if (_usuario == null) return;

            TxtNombre.Text = _usuario.name;
            TxtApellidos.Text = _usuario.apellidos;
            TxtEmail.Text = _usuario.email;
            TxtTelefono.Text = _usuario.telefono;

            // Seleccionar rol en combobox si coincide
            for (int i = 0; i < CmbRol.Items.Count; i++)
            {
                if (CmbRol.Items[i] is ComboBoxItem item && item.Content.ToString() == _usuario.rol)
                {
                    CmbRol.SelectedIndex = i;
                    break;
                }
            }
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

            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                TxtErrorNombre.Text = "El campo nombre es obligatorio.";
                TxtErrorNombre.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (string.IsNullOrWhiteSpace(TxtApellidos.Text))
            {
                TxtErrorApellidos.Text = "El campo apellidos es obligatorio.";
                TxtErrorApellidos.Visibility = Visibility.Visible;
                hayErrores = true;
            }

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

            if (CmbRol.SelectedIndex <= 0)
            {
                TxtErrorRol.Text = "Debe seleccionar un rol válido.";
                TxtErrorRol.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            // Si contraseña proporcionada, validar longitud y confirmación
            if (!string.IsNullOrWhiteSpace(TxtPassword.Password) && TxtPassword.Password.Length < 8)
            {
                TxtErrorPassword.Text = "La contraseña debe tener al menos 8 caracteres.";
                TxtErrorPassword.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (TxtPassword.Password != TxtPasswordConfirm.Password)
            {
                TxtErrorPasswordConfirm.Text = "Las contraseñas no coinciden.";
                TxtErrorPasswordConfirm.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (hayErrores)
            {
                await MostrarAlerta("Error de validación", "Uno o más campos son incorrectos.");
                return;
            }

            string rolSeleccionado = ((ComboBoxItem)CmbRol.SelectedItem).Content.ToString();

            var usuarioUpdate = new System.Dynamic.ExpandoObject() as dynamic;
            usuarioUpdate.name = TxtNombre.Text.Trim();
            usuarioUpdate.apellidos = TxtApellidos.Text.Trim();
            usuarioUpdate.email = TxtEmail.Text.Trim();
            usuarioUpdate.rol = rolSeleccionado;
            usuarioUpdate.telefono = TxtTelefono.Text.Trim();

            if (!string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                usuarioUpdate.password = TxtPassword.Password;
            }

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiConfig.BaseUrl}users{_usuario.id}")
;
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {SessionManager.AuthToken}");

                var jsonContent = JsonSerializer.Serialize(usuarioUpdate);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await MostrarAlerta("Éxito", "¡Usuario actualizado exitosamente!");
                    // Volver a la lista
                    if (this.NavigationService.CanGoBack) this.NavigationService.GoBack();
                    else this.NavigationService.Navigate(new System.Uri("/Views/Users/UsuariosPage.xaml", System.UriKind.Relative));
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    if (errorResponse.Contains("email"))
                    {
                        TxtErrorEmail.Text = "Este correo electrónico ya está registrado.";
                        TxtErrorEmail.Visibility = Visibility.Visible;
                    }

                    await MostrarAlerta("Error de servidor", "No se pudo actualizar el usuario. Verifique los datos.");
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
