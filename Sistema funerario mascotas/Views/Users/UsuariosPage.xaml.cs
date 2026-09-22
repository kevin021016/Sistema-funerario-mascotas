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
using System.IO;
using Microsoft.Win32;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sistema_funerario_mascotas.Views.Users;

namespace Sistema_funerario_mascotas
{
    /// <summary>
    /// Lógica de interacción para UsuariosPage.xaml
    /// </summary>
    public partial class UsuariosPage : Page
    {
        public UsuariosPage()
        {
            InitializeComponent();
        }

        private List<User> GetUsers()
        {
            return new List<User>
            {
                new User { Nombre = "Kevin", Apellidos = "Aviles", Email = "kevinmaniacao117@hotmail.com", UltimaVezActivo = "N/A" },
                new User { Nombre = "Hugo", Apellidos = "Sánchez", Email = "h.sanchez@test.com", UltimaVezActivo = "2026-05-24 00:53:10" },
                new User { Nombre = "Marcos", Apellidos = "Peralta", Email = "m.peralta@test.com", UltimaVezActivo = "2026-05-11 20:08:30" }
            };
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var users = GetUsers();

            var dlg = new SaveFileDialog();
            dlg.Filter = "Excel Workbook|*.xlsx";
            dlg.FileName = "usuarios.xlsx";
            if (dlg.ShowDialog() != true) return;

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Usuarios");
                ws.Cell(1, 1).Value = "Nombre";
                ws.Cell(1, 2).Value = "Apellidos";
                ws.Cell(1, 3).Value = "Email";
                ws.Cell(1, 4).Value = "Última vez activo";

                for (int i = 0; i < users.Count; i++)
                {
                    var row = i + 2;
                    ws.Cell(row, 1).Value = users[i].Nombre;
                    ws.Cell(row, 2).Value = users[i].Apellidos;
                    ws.Cell(row, 3).Value = users[i].Email;
                    ws.Cell(row, 4).Value = users[i].UltimaVezActivo;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var users = GetUsers();

            var dlg = new SaveFileDialog();
            dlg.Filter = "PDF file|*.pdf";
            dlg.FileName = "usuarios.pdf";
            if (dlg.ShowDialog() != true) return;

            // Generar PDF simple con QuestPDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("Lista de usuarios").FontSize(18).Bold();
                        col.Item().PaddingVertical(5);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Nombre");
                                header.Cell().Element(CellStyle).Text("Apellidos");
                                header.Cell().Element(CellStyle).Text("Email");
                                header.Cell().Element(CellStyle).Text("Última vez activo");
                            });

                            foreach (var u in users)
                            {
                                table.Cell().Element(CellStyle).Text(u.Nombre);
                                table.Cell().Element(CellStyle).Text(u.Apellidos);
                                table.Cell().Element(CellStyle).Text(u.Email);
                                table.Cell().Element(CellStyle).Text(u.UltimaVezActivo);
                            }
                        });
                    });
                });
            });

            // Guardar
            document.GeneratePdf(dlg.FileName);
        }

        private IContainer CellStyle(IContainer container)
        {
            return container.Padding(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4);
        }

        private void IrAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Asumiendo que tu página de registro se llama "AgregarUsuarioPage"
            // Esto busca el contenedor principal y navega hacia la nueva página
            this.NavigationService.Navigate(new CrearUsuarioPage());
        }

        private class User
        {
            public string Nombre { get; set; }
            public string Apellidos { get; set; }
            public string Email { get; set; }
            public string UltimaVezActivo { get; set; }
        }
    }
}
