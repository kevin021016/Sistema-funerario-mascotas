using Sistema_funerario_mascotas.Views;
using System.Net.Http;
using System.Net.Http;
using System.Text;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sistema_funerario_mascotas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Carga automáticamente el DashboardPage al iniciar
            RootNavigation.Navigate(typeof(DashboardPage));
        }

        private async void Logout(object sender, RoutedEventArgs e)
        {
            var confirmWindow = new ConfirmDialogWindow("Confirmar Cierre de Sesión", "¿Está seguro de que desea cerrar sesión?")
            {
                Owner = Application.Current.MainWindow
            };

            confirmWindow.ShowDialog();

            if (confirmWindow.Confirmed)
            {
                try
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.AuthToken);

                    var response = await client.PostAsync($"{ApiConfig.BaseUrl}logout", null);

                    // Independientemente de si la API responde con éxito o hay algún detalle, 
                    // limpiamos la sesión localmente por seguridad
                    SessionManager.AuthToken = string.Empty;
                    SessionManager.UserName = string.Empty;
                    SessionManager.UserId = string.Empty;

                    // Abrimos la ventana de login y cerramos la principal
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();

                    // Cerramos todas las ventanas abiertas actuales
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != loginWindow)
                        {
                            window.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Si falla la red, de todos modos forzamos la salida localmente
                    SessionManager.AuthToken = string.Empty;
                    SessionManager.UserName = string.Empty;
                    SessionManager.UserId = string.Empty;

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window != loginWindow)
                        {
                            window.Close();
                        }
                    }
                }
            }
        }
    }
}