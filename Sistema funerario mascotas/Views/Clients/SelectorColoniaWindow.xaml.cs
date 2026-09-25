using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sistema_funerario_mascotas.Views.Clients
{
    public partial class SelectorColoniaWindow : Window
    {
        private List<ColoniaModel> _todasLasColonias = new List<ColoniaModel>();
        public ColoniaModel ColoniaSeleccionada { get; private set; }

        public SelectorColoniaWindow()
        {
            InitializeComponent();
            CargarColoniasCompletas();
        }

        private async void CargarColoniasCompletas()
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
                    _todasLasColonias = JsonSerializer.Deserialize<List<ColoniaModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ColoniaModel>();
                    LvColonias.ItemsSource = _todasLasColonias;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar colonias: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscadorColonia_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = TxtBuscadorColonia.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(filtro))
            {
                LvColonias.ItemsSource = _todasLasColonias;
            }
            else
            {
                LvColonias.ItemsSource = _todasLasColonias.Where(c =>
                    (c.nombre != null && c.nombre.ToLower().Contains(filtro)) ||
                    (c.codigo_postal != null && c.codigo_postal.Contains(filtro))
                ).ToList();
            }
        }

        private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
        {
            if (LvColonias.SelectedItem is ColoniaModel seleccionada)
            {
                ColoniaSeleccionada = seleccionada;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor seleccione una colonia de la lista.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LvColonias_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LvColonias.SelectedItem is ColoniaModel seleccionada)
            {
                ColoniaSeleccionada = seleccionada;
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}