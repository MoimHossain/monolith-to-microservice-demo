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
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly ProductTable productTable;

        public ProductController(ILogger<ProductController> logger, ProductTable productTable)
        {
            _logger = logger;
            this.productTable = productTable;
        }

        public IActionResult Index()
        {

            return View();
        }

        public IActionResult NewProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] Product product)
        {
            await productTable.CreateProductAsync(product);

            return new JsonResult(product);
        }

        public async  Task<List<Product>> GetAll()
        {
            return await productTable.GetAllAsync();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
