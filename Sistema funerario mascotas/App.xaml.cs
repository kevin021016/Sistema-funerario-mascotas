using System.Configuration;
using System.Data;
using System.Windows;

namespace Sistema_funerario_mascotas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Muestra la ventana de login al iniciar la aplicación
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }

}
