using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Context;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace MyStoreLaZeta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

  
            public IActionResult Index(string tipo = "diario")
            {
            // 1. Traemos los pedidos de la base de datos (último año para tener margen)
            var fechaLimite = DateTime.Now.AddYears(-1);
            var pedidos = _context.Orders
                .Where(o => o.OrderDate >= fechaLimite)
                .ToList(); // Traemos a memoria para agrupar fácil en C#

            // Variables para el gráfico
            string[] etiquetas = null;
            decimal[] valores = null;

            // 2. Lógica de agrupación según el botón presionado
            switch (tipo.ToLower())
            {
                case "mensual":
                    var reporteMensual = pedidos
                        .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                        .Select(g => new {
                            Fecha = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Total = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Fecha)
                        .ToList();

                    etiquetas = reporteMensual.Select(x => x.Fecha.ToString("MMM yyyy")).ToArray();
                    valores = reporteMensual.Select(x => x.Total).ToArray();
                    ViewBag.TituloGrafico = "Ventas por Mes (Último Año)";
                    break;

                case "semanal":
                    // Truco: Agrupamos por el "Inicio de la semana" (Domingo/Lunes)
                    var reporteSemanal = pedidos
                        .GroupBy(o => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            o.OrderDate,
                            System.Globalization.CalendarWeekRule.FirstDay,
                            DayOfWeek.Monday))
                        .Select(g => new {
                            Semana = "Semana " + g.Key,
                            Total = g.Sum(o => o.TotalAmount)
                        })
                        .ToList();

                    etiquetas = reporteSemanal.Select(x => x.Semana).ToArray();
                    valores = reporteSemanal.Select(x => x.Total).ToArray();
                    ViewBag.TituloGrafico = "Ventas por Semana";
                    break;

                case "diario":
                default:
                    // Tu lógica original (últimos 15 días para que no se sature el gráfico)
                    var reporteDiario = pedidos
                        .Where(o => o.OrderDate >= DateTime.Now.AddDays(-15))
                        .GroupBy(o => o.OrderDate.Date)
                        .Select(g => new {
                            Fecha = g.Key,
                            Total = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Fecha)
                        .ToList();

                    etiquetas = reporteDiario.Select(x => x.Fecha.ToString("dd/MM")).ToArray();
                    valores = reporteDiario.Select(x => x.Total).ToArray();
                    ViewBag.TituloGrafico = "Ventas Diarias (Últimos 15 días)";
                    break;
            }

            // 3. Pasamos los datos a la Vista
            ViewBag.Etiquetas = etiquetas;
            ViewBag.Valores = valores;

            // Para saber qué botón pintar de activo
            ViewBag.TipoActivo = tipo;

            // KPIs Generales (Siempre históricos)
            ViewBag.TotalIngresos = _context.Orders.Sum(o => o.TotalAmount);
            ViewBag.CantidadPedidos = _context.Orders.Count();

            return View();
        
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            // 1. Traemos todos los datos (igual que en tus otras consultas)
            var pedidos = await _context.Orders
                .Include(o => o.OrderItems) // Incluimos items por si quieres detallar
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // 2. Creamos el archivo Excel en memoria
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Ventas");

                // 3. Encabezados (Cabecera de la tabla)
                worksheet.Cell(1, 1).Value = "#ID";
                worksheet.Cell(1, 2).Value = "Fecha";
                worksheet.Cell(1, 3).Value = "Cliente";
                worksheet.Cell(1, 4).Value = "Email";
                worksheet.Cell(1, 5).Value = "Total";
                worksheet.Cell(1, 6).Value = "Estado";
                worksheet.Cell(1, 7).Value = "Método Pago";

                // Le damos estilo negrita a la cabecera
                var rangoCabecera = worksheet.Range("A1:G1");
                rangoCabecera.Style.Font.Bold = true;
                rangoCabecera.Style.Fill.BackgroundColor = XLColor.FromHtml("#17A09C"); // Tu color Zeta Cyan
                rangoCabecera.Style.Font.FontColor = XLColor.White;

                // 4. Llenamos las filas con los datos
                int fila = 2;
                foreach (var pedido in pedidos)
                {
                    worksheet.Cell(fila, 1).Value = pedido.OrderId;
                    worksheet.Cell(fila, 2).Value = pedido.OrderDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(fila, 3).Value = pedido.ClientName;
                    worksheet.Cell(fila, 4).Value = pedido.Email;
                    worksheet.Cell(fila, 5).Value = pedido.TotalAmount; // Números directos
                    worksheet.Cell(fila, 6).Value = pedido.Status;
                    worksheet.Cell(fila, 7).Value = pedido.PaymentMethod;

                    // Formato de moneda para la columna Total
                    worksheet.Cell(fila, 5).Style.NumberFormat.Format = "$ #,##0.00";

                    fila++;
                }

                // Autoajustar el ancho de las columnas
                worksheet.Columns().AdjustToContents();

                // 5. Preparamos la descarga
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Reporte_Ventas_{DateTime.Now:ddMMyyyy}.xlsx"
                    );
                }
            }
        }
    }
}


