using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Clients
{
    public partial class EditarClientePage : Page
    {
        private ClienteModel _cliente;
        private bool _isUpdatingFecha = false;

        public EditarClientePage(ClienteModel cliente)
        {
            InitializeComponent();
            _cliente = cliente;
            CargarColoniasYDatos();
        }

        private async void CargarColoniasYDatos()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}colonias");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var colonias = JsonSerializer.Deserialize<List<ColoniaModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (colonias != null)
                    {
                        colonias.Insert(0, new ColoniaModel { id = 0, nombre = "-- Seleccione una colonia --" });
                        CmbColonia.ItemsSource = colonias;
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarAlerta("Aviso", $"No se pudieron cargar las colonias: {ex.Message}");
            }

            // Rellenar campos con los datos actuales del cliente después de cargar el ComboBox
            if (_cliente != null)
            {
                TxtNombre.Text = _cliente.nombre;
                TxtApellidos.Text = _cliente.apellidos;
                TxtCalle.Text = _cliente.calle;
                TxtEntreCalles.Text = _cliente.entre_calles;
                TxtCiudad.Text = _cliente.ciudad;
                TxtReferencia.Text = _cliente.referencia_domicilio;
                TxtTelefono.Text = _cliente.telefono;
                TxtCelular.Text = _cliente.celular;

                if (!string.IsNullOrEmpty(_cliente.fecha_nacimiento))
                {
                    if (DateTime.TryParse(_cliente.fecha_nacimiento, out DateTime fechaParsed))
                    {
                        TxtFechaNacimiento.Text = fechaParsed.ToString("dd/MM/yyyy");
                    }
                }

                if (_cliente.colonia_id > 0)
                {
                    CmbColonia.SelectedValue = _cliente.colonia_id;
                }
                else
                {
                    CmbColonia.SelectedIndex = 0;
                }
            }
        }

        private void Numero_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Fecha_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtFechaNacimiento_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFecha) return;

            var textBox = sender as Wpf.Ui.Controls.TextBox;
            if (textBox == null) return;

            string text = Regex.Replace(textBox.Text, "[^0-9]", "");
            if (text.Length > 8) text = text.Substring(0, 8);

            string day = "", month = "", year = "";

            if (text.Length >= 1)
            {
                char d1 = text[0];
                if (d1 > '3')
                {
                    day = "0" + d1;
                    if (text.Length >= 2) month = text.Substring(1, Math.Min(1, text.Length - 1));
                    if (text.Length >= 3) month += text.Substring(2, Math.Min(1, text.Length - 2));
                    if (text.Length >= 4) year = text.Substring(3);
                }
                else
                {
                    day = text.Substring(0, Math.Min(2, text.Length));
                    if (text.Length >= 3)
                    {
                        char m1 = text[2];
                        if (m1 > '1')
                        {
                            month = "0" + m1;
                            if (text.Length >= 4) year = text.Substring(3);
                        }
                        else
                        {
                            month = text.Substring(2, Math.Min(2, text.Length - 2));
                            if (text.Length >= 5) year = text.Substring(4);
                        }
                    }
                }
            }

            if (int.TryParse(day, out int dVal))
            {
                if (dVal > 31) day = "31";
                else if (dVal == 0 && day.Length == 2) day = "01";
            }

            if (int.TryParse(month, out int mVal))
            {
                if (mVal > 12) month = "12";
                else if (mVal == 0 && month.Length == 2) month = "01";
            }

            string formatted = day;
            if (day.Length == 2 || text.Length > 2) formatted += "/";
            formatted += month;
            if (month.Length == 2 || (text.Length >= 5 && month.Length == 2)) formatted += "/";
            formatted += year;

            _isUpdatingFecha = true;
            textBox.Text = formatted;
            textBox.CaretIndex = formatted.Length;
            _isUpdatingFecha = false;
        }

        private void Texto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string textoPegado = (string)e.DataObject.GetData(DataFormats.Text);
                Regex regex = new Regex("[^0-9]+");
                if (regex.IsMatch(textoPegado)) e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void BtnAbrirBuscadorColonia_Click(object sender, RoutedEventArgs e)
        {
            var selectorWindow = new SelectorColoniaWindow
            {
                Owner = Application.Current.MainWindow
            };

            if (selectorWindow.ShowDialog() == true && selectorWindow.ColoniaSeleccionada != null)
            {
                var colonia = selectorWindow.ColoniaSeleccionada;

                if (CmbColonia.ItemsSource is List<ColoniaModel> lista)
                {
                    if (!lista.Exists(c => c.id == 0))
                    {
                        lista.Insert(0, new ColoniaModel { id = 0, nombre = "-- Seleccione una colonia --" });
                    }

                    if (!lista.Exists(c => c.id == colonia.id))
                    {
                        lista.Add(colonia);
                        CmbColonia.ItemsSource = null;
                        CmbColonia.ItemsSource = lista;
                    }
                }
                CmbColonia.SelectedValue = colonia.id;
            }
        }

        private void LimpiarErroresUI()
        {
            TxtErrorNombre.Visibility = Visibility.Hidden;
            TxtErrorApellidos.Visibility = Visibility.Hidden;
            TxtErrorFechaNacimiento.Visibility = Visibility.Hidden;
            TxtErrorCalle.Visibility = Visibility.Hidden;
            TxtErrorColonia.Visibility = Visibility.Hidden;
            TxtErrorCiudad.Visibility = Visibility.Hidden;
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarErroresUI();
            bool hayErrores = false;

            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                TxtErrorNombre.Text = "El nombre es obligatorio.";
                TxtErrorNombre.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (string.IsNullOrWhiteSpace(TxtApellidos.Text))
            {
                TxtErrorApellidos.Text = "Los apellidos son obligatorios.";
                TxtErrorApellidos.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            string fechaNacimientoFormateada = null;
            if (string.IsNullOrWhiteSpace(TxtFechaNacimiento.Text) || TxtFechaNacimiento.Text.Length < 10)
            {
                TxtErrorFechaNacimiento.Text = "La fecha es obligatoria y debe ser completa (DD/MM/AAAA).";
                TxtErrorFechaNacimiento.Visibility = Visibility.Visible;
                hayErrores = true;
            }
            else
            {
                string textoFecha = TxtFechaNacimiento.Text.Trim();
                if (DateTime.TryParseExact(textoFecha, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaParsed))
                {
                    if (fechaParsed.Year <= 1900 || fechaParsed.Year > DateTime.Now.Year)
                    {
                        TxtErrorFechaNacimiento.Text = "El año debe ser mayor a 1900 y válido.";
                        TxtErrorFechaNacimiento.Visibility = Visibility.Visible;
                        hayErrores = true;
                    }
                    else
                    {
                        fechaNacimientoFormateada = fechaParsed.ToString("yyyy-MM-dd");
                    }
                }
                else
                {
                    TxtErrorFechaNacimiento.Text = "Fecha inválida.";
                    TxtErrorFechaNacimiento.Visibility = Visibility.Visible;
                    hayErrores = true;
                }
            }

            if (string.IsNullOrWhiteSpace(TxtCalle.Text))
            {
                TxtErrorCalle.Text = "La calle es obligatoria.";
                TxtErrorCalle.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (CmbColonia.SelectedValue == null || (int)CmbColonia.SelectedValue == 0)
            {
                TxtErrorColonia.Text = "Debe seleccionar una colonia.";
                TxtErrorColonia.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (string.IsNullOrWhiteSpace(TxtCiudad.Text))
            {
                TxtErrorCiudad.Text = "La ciudad es obligatoria.";
                TxtErrorCiudad.Visibility = Visibility.Visible;
                hayErrores = true;
            }

            if (hayErrores)
            {
                await MostrarAlerta("Error de validación", "Complete los campos obligatorios.");
                return;
            }

            var clienteUpdate = new
            {
                nombre = TxtNombre.Text.Trim(),
                apellidos = TxtApellidos.Text.Trim(),
                fecha_nacimiento = fechaNacimientoFormateada,
                calle = TxtCalle.Text.Trim(),
                entre_calles = TxtEntreCalles.Text.Trim(),
                colonia_id = (int)CmbColonia.SelectedValue,
                ciudad = TxtCiudad.Text.Trim(),
                referencia_domicilio = TxtReferencia.Text.Trim(),
                telefono = TxtTelefono.Text.Trim(),
                celular = TxtCelular.Text.Trim()
            };

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiConfig.BaseUrl}clients/{_cliente.id}");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var jsonContent = JsonSerializer.Serialize(clienteUpdate);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await MostrarAlerta("Éxito", "¡Cliente actualizado exitosamente!");
                    BtnVolver_Click(sender, e);
                }
                else
                {
                    await MostrarAlerta("Error de servidor", "No se pudo actualizar el cliente.");
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
            if (this.NavigationService.CanGoBack) this.NavigationService.GoBack();
            else this.NavigationService.Navigate(new ClientesPage());
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => BtnVolver_Click(sender, e);

        private void BreadcrumbClientes_Click(object sender, MouseButtonEventArgs e) => this.NavigationService.Navigate(new ClientesPage());

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(DashboardPage));
            }
        }
    }
}