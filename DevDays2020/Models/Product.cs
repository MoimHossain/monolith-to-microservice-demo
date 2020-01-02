using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevDays2020.Models
{
    public class Product
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Descroption { get; set; }
        public double Price { get; set; }
    }

    public class SalesOrder
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }

    public class LineItem
    {
        public Guid OrderID { get; set; }
        public Guid ID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }
}
