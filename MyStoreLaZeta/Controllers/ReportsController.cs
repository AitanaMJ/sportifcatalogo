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

        public IActionResult Index(string tipo = "diario", string mesFiltro = "")
        {
            // Configuración de fechas para el filtro
            DateTime fechaInicio;
            DateTime fechaFin;

            if (string.IsNullOrEmpty(mesFiltro))
            {
                fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
                mesFiltro = DateTime.Now.ToString("yyyy-MM");
            }
            else
            {
                fechaInicio = DateTime.Parse(mesFiltro + "-01");
                fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
            }

            // Datos para el gráfico principal (Líneas)
            var fechaLimiteGrafico = DateTime.Now.AddYears(-1);
            var pedidosTotales = _context.Orders
                .Where(o => o.OrderDate >= fechaLimiteGrafico)
                .ToList();

            string[] etiquetas = null;
            decimal[] valores = null;

            switch (tipo.ToLower())
            {
                case "mensual":
                    var reporteMensual = pedidosTotales
                        .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                        .Select(g => new {
                            Fecha = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Total = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Fecha).ToList();
                    etiquetas = reporteMensual.Select(x => x.Fecha.ToString("MMM yyyy")).ToArray();
                    valores = reporteMensual.Select(x => x.Total).ToArray();
                    ViewBag.TituloGrafico = "Ventas por Mes (Último Año)";
                    break;

                case "semanal":
                    var reporteSemanal = pedidosTotales
                        .GroupBy(o => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(o.OrderDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                        .Select(g => new { Semana = "Sem " + g.Key, Total = g.Sum(o => o.TotalAmount) }).ToList();
                    etiquetas = reporteSemanal.Select(x => x.Semana).ToArray();
                    valores = reporteSemanal.Select(x => x.Total).ToArray();
                    ViewBag.TituloGrafico = "Ventas por Semana";
                    break;

                case "diario":
                default:
                    var reporteDiario = pedidosTotales
                        .Where(o => o.OrderDate >= fechaInicio && o.OrderDate <= fechaFin)
                        .GroupBy(o => o.OrderDate.Date)
                        .Select(g => new { Fecha = g.Key, Total = g.Sum(o => o.TotalAmount) })
                        .OrderBy(x => x.Fecha).ToList();
                    etiquetas = reporteDiario.Select(x => x.Fecha.ToString("dd/MM")).ToArray();
                    valores = reporteDiario.Select(x => x.Total).ToArray();
                    ViewBag.TituloGrafico = $"Ventas Diarias - {fechaInicio:MMMM yyyy}";
                    break;
            }

            // Lógica para el Gráfico de Torta (Categorías) del mes seleccionado
            var ventasPorCategoria = _context.OrderItems
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.Order)
                .Where(i => i.Order!.OrderDate >= fechaInicio && i.Order.OrderDate <= fechaFin)
                .GroupBy(i => i.Product!.Category!.Name)
                .Select(g => new { Categoria = g.Key, Total = g.Sum(i => i.Price * i.Quantity) })
                .ToList();

            ViewBag.CategoriasEtiquetas = ventasPorCategoria.Select(x => x.Categoria).ToArray();
            ViewBag.CategoriasValores = ventasPorCategoria.Select(x => x.Total).ToArray();

            // KPIs y ViewBags finales
            var pedidosMes = _context.Orders.Where(o => o.OrderDate >= fechaInicio && o.OrderDate <= fechaFin);
            ViewBag.TotalIngresos = pedidosMes.Sum(o => o.TotalAmount);
            ViewBag.CantidadPedidos = pedidosMes.Count();
            ViewBag.Etiquetas = etiquetas;
            ViewBag.Valores = valores;
            ViewBag.TipoActivo = tipo;
            ViewBag.MesFiltro = mesFiltro;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string mesFiltro = "")
        {
            var query = _context.Orders.AsQueryable();
            if (!string.IsNullOrEmpty(mesFiltro))
            {
                DateTime fechaInicio = DateTime.Parse(mesFiltro + "-01");
                DateTime fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
                query = query.Where(o => o.OrderDate >= fechaInicio && o.OrderDate <= fechaFin);
            }

            var pedidos = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte");
                worksheet.Cell(1, 1).Value = "Fecha";
                worksheet.Cell(1, 2).Value = "Cliente";
                worksheet.Cell(1, 3).Value = "Total";

                int fila = 2;
                foreach (var p in pedidos)
                {
                    worksheet.Cell(fila, 1).Value = p.OrderDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(fila, 2).Value = p.ClientName;
                    worksheet.Cell(fila, 3).Value = p.TotalAmount;
                    fila++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteLaZeta.xlsx");
                }
            }
        }
    }
}

