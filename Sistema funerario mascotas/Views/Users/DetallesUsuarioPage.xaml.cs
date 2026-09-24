using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Users
{
    public partial class DetallesUsuarioPage : Page
    {
        private UsuarioModel _usuario;
        private int _paginaActual = 1;
        private int _ultimaPagina = 1;

        public DetallesUsuarioPage(UsuarioModel usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            CargarInformacionUsuario();
            VerificarYCargarLogs();
        }

        private void CargarInformacionUsuario()
        {
            if (_usuario == null) return;

            TxtNombre.Text = _usuario.name;
            TxtApellidos.Text = _usuario.apellidos;
            TxtEmail.Text = _usuario.email;
            TxtTelefono.Text = _usuario.telefono;
            TxtRol.Text = _usuario.rol;
        }

        private async void VerificarYCargarLogs()
        {
            if (_usuario == null) return;

            // Validar si el usuario es Administrador
            if (_usuario.rol != "Administrador")
            {
                PnlLogsContainer.Visibility = Visibility.Collapsed;
                TxtNoAdministrador.Visibility = Visibility.Visible;
                return;
            }

            // Si es Administrador, cargamos su historial de logs
            await CargarLogsPagina(_paginaActual);
        }

        private async System.Threading.Tasks.Task CargarLogsPagina(int pagina)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}users/{_usuario.id}/logs?page={pagina}");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Deserializamos el objeto de paginación de Laravel
                    var paginatedResponse = JsonSerializer.Deserialize<PaginatedLogsResponse>(jsonString, opciones);

                    if (paginatedResponse != null)
                    {
                        IcLogs.ItemsSource = paginatedResponse.data;
                        _paginaActual = paginatedResponse.current_page;
                        _ultimaPagina = paginatedResponse.last_page;

                        TxtPaginaActual.Text = $"Página {_paginaActual} de {_ultimaPagina}";

                        // Habilitar o deshabilitar botones de paginación
                        BtnAnterior.IsEnabled = _paginaActual > 1;
                        BtnSiguiente.IsEnabled = _paginaActual < _ultimaPagina;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los movimientos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual > 1)
            {
                _paginaActual--;
                await CargarLogsPagina(_paginaActual);
            }
        }

        private async void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (_paginaActual < _ultimaPagina)
            {
                _paginaActual++;
                await CargarLogsPagina(_paginaActual);
            }
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

    // Modelos auxiliares para mapear la respuesta paginada de Laravel
    public class PaginatedLogsResponse
    {
        public int current_page { get; set; }
        public List<ActivityLogModel> data { get; set; }
        public int last_page { get; set; }
    }

    public class ActivityLogModel
    {
        public int id { get; set; }
        public string action { get; set; }
        public string subject { get; set; }
        public string description { get; set; }
        public string created_at { get; set; }

        public string FechaLegible
        {
            get
            {
                if (DateTime.TryParse(created_at, out DateTime fechaParsed))
                {
                    // Mostramos la hora exacta que viene de la BD sin conversiones extra
                    return fechaParsed.ToString("yyyy-MM-dd HH:mm:ss");
                }
                return created_at;
            }
        }
    }
}