using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DevDays2020.Models;
using DevDays2020.Data;

namespace DevDays2020.Controllers
{
    public class SalesOrderController : Controller
    {
        private readonly ILogger<SalesOrderController> _logger;
        private readonly SalesOrderTable salesOrderTable;
        public SalesOrderController(ILogger<SalesOrderController> logger, SalesOrderTable salesOrderTable)
        {
            _logger = logger;
            this.salesOrderTable = salesOrderTable;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult NewSO()
        {
            return View();
        }

        public async Task<IActionResult> Show(Guid id)
        {
            ViewData["data"] = await salesOrderTable.GetSOAsync(id);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] SalesOrderVM so)
        {
            await salesOrderTable.CreateSOAsync(new SalesOrder
            {
                Name = so.Name,
                Price = so.Items.Sum(s => s.Price)
            }, so.Items); 

            return new JsonResult(new { Ok = true });
        }

        public async Task<List<SalesOrder>> GetAll()
        {
            return await salesOrderTable.GetAllAsync();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class SalesOrderVM
    {
        public string Name { get; set; }
        public List<LineItem> Items { get; set; }
    }
}
