using Sistema_funerario_mascotas.Views.Clients;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sistema_funerario_mascotas.Views.Reportes
{
    /// <summary>
    /// Lógica de interacción para ReportesPage.xaml
    /// </summary>
    public partial class ReportesPage : Page
    {
        public ReportesPage()
        {
            InitializeComponent();
        }

        private void IrReportes_Usuarios(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ReportesUsuariosPage());
        }
    }
}
