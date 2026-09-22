using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sistema_funerario_mascotas.Views.Users
{
    /// <summary>
    /// Lógica de interacción para CrearUsuarioPage.xaml
    /// </summary>
    public partial class CrearUsuarioPage : Page
    {
        public CrearUsuarioPage()
        {
            InitializeComponent();
        }

        // 1. Evita que se escriban letras u otros símbolos al presionar teclas
        private void Numero_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); // Solo permite números del 0 al 9
            e.Handled = regex.IsMatch(e.Text);
        }

        // 2. Evita que peguen texto que contenga letras o símbolos mediante Ctrl+V o menú contextual
        private void Texto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string textoPegado = (string)e.DataObject.GetData(DataFormats.Text);
                Regex regex = new Regex("[^0-9]+");

                // Si el texto pegado contiene algo que no sea número, se cancela la acción de pegar
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
    }
}
