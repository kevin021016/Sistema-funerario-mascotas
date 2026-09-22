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

namespace Sistema_funerario_mascotas.Views
{
    /// <summary>
    /// Lógica de interacción para CustomMessageWindow.xaml
    /// </summary>
    public partial class CustomMessageWindow : Wpf.Ui.Controls.FluentWindow
    {
        public CustomMessageWindow(string title, string message)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
