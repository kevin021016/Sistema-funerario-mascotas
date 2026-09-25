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
    public partial class CrearClientePage : Page
    {
        public CrearClientePage()
        {
            InitializeComponent();
            CargarColonias();
        }

        private async void CargarColonias()
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
                        // Insertar opción por defecto al inicio de la lista
                        colonias.Insert(0, new ColoniaModel { id = 0, nombre = "-- Seleccione una colonia --" });

                        CmbColonia.ItemsSource = colonias;
                        CmbColonia.SelectedIndex = 0; // Seleccionar por defecto la opción 0
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarAlerta("Aviso", $"No se pudieron cargar las colonias: {ex.Message}");
            }
        }

        // Validación para permitir solo números en teléfonos
        private void Numero_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Autocompletar barras y restringir caracteres en la fecha
        private bool _isUpdatingFecha = false;

        private void Fecha_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir números
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtFechaNacimiento_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFecha) return;

            var textBox = sender as Wpf.Ui.Controls.TextBox;
            if (textBox == null) return;

            // Extraer solo los números ingresados
            string text = Regex.Replace(textBox.Text, "[^0-9]", "");
            if (text.Length > 8) text = text.Substring(0, 8);

            string day = "", month = "", year = "";

            if (text.Length >= 1)
            {
                char d1 = text[0];
                // Si el usuario escribe un número mayor a 3 al inicio del día (ej. 4, 5, 6...), 
                // asumimos que quiso escribir un día del 01 al 09 y autocompletamos con "0".
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
                        // Si el primer dígito del mes es mayor a 1 (ej. 2, 3...), autocompletamos con "0".
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

            // Validaciones estrictas de límites numéricos en tiempo real
            if (int.TryParse(day, out int dVal))
            {
                if (dVal > 31) day = "31"; // Evita días mayores a 31 (como 32 o 40)
                else if (dVal == 0 && day.Length == 2) day = "01";
            }

            if (int.TryParse(month, out int mVal))
            {
                if (mVal > 12) month = "12"; // Evita meses mayores a 12
                else if (mVal == 0 && month.Length == 2) month = "01";
            }

            // Construir la cadena de texto formateada con las barras diagonales automáticas
            string formatted = day;
            if (day.Length == 2 || text.Length > 2)
            {
                formatted += "/";
            }
            formatted += month;
            if (month.Length == 2 || (text.Length >= 5 && month.Length == 2))
            {
                formatted += "/";
            }
            formatted += year;

            _isUpdatingFecha = true;
            textBox.Text = formatted;
            textBox.CaretIndex = formatted.Length; // Mantiene el cursor al final de forma fluida
            _isUpdatingFecha = false;
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
                    // Asegurar que el elemento por defecto siempre exista en la lista
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

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Ocultar errores previos
            TxtErrorNombre.Visibility = Visibility.Hidden;
            TxtErrorApellidos.Visibility = Visibility.Hidden;
            TxtErrorFechaNacimiento.Visibility = Visibility.Hidden;
            TxtErrorCalle.Visibility = Visibility.Hidden;
            TxtErrorColonia.Visibility = Visibility.Hidden;
            TxtErrorCiudad.Visibility = Visibility.Hidden;

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

            // Validar Fecha de Nacimiento obligatoria, completa y con lógica estricta de calendario
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

                // TryParseExact valida automáticamente días del mes (31, 30, 28/29 febrero) y meses (1-12)
                if (DateTime.TryParseExact(textoFecha, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaParsed))
                {
                    // Validar que el año tenga sentido (mayor a 1900 y no en el futuro)
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
                    TxtErrorFechaNacimiento.Text = "Fecha inválida (verifique días del mes, mes máximo 12 o formato).";
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

            // Validar que se haya seleccionado una colonia válida (distinta de 0 o nula)
            if (CmbColonia.SelectedValue == null || (int)CmbColonia.SelectedValue == 0)
            {
                TxtErrorColonia.Text = "Debe seleccionar una colonia obligatoriamente.";
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
                await MostrarAlerta("Error de validación", "Complete los campos obligatorios marcados.");
                return;
            }

            var nuevoCliente = new
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
                var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiConfig.BaseUrl}clients");

                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var jsonContent = JsonSerializer.Serialize(nuevoCliente);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await MostrarAlerta("Éxito", "¡Cliente creado exitosamente!");
                    BtnVolver_Click(sender, e);
                }
                else
                {
                    await MostrarAlerta("Error de servidor", "No se pudo guardar el cliente. Verifique los datos.");
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
                this.NavigationService.Navigate(new ClientesPage());
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnVolver_Click(sender, e);
        }

        private void BreadcrumbClientes_Click(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new ClientesPage());
        }

        private void BreadcrumbDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.RootNavigation.Navigate(typeof(DashboardPage));
            }
        }
    }

    public class ColoniaModel
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string codigo_postal { get; set; }
    }
}