using System.Windows;

namespace Sistema_funerario_mascotas.Views
{
    public partial class ConfirmDialogWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool Confirmed { get; private set; } = false;

        public ConfirmDialogWindow(string title, string message)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
        }

        private void BtnSi_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            this.Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            this.Close();
        }
    }
}