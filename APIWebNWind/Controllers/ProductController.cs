using Microsoft.AspNetCore.Mvc;
using APIWebNWind.Data;
using APIWebNWind.Models;

using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static NuGet.Packaging.PackagingConstants;
using Google.Protobuf.WellKnownTypes;

namespace APIWebNWind.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : Controller
    {
        private readonly NorthwindContext contexto;

        public ProductController(NorthwindContext context)
        {
            contexto = context;
        }

        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return contexto.Products.OrderBy(p => p.ProductName);

        }

        /*
         Obtener el nombre del producto, categoría (nombre), existencia de todos los productos que estan activos con el uso de JOIN
       
         */

        [HttpGet]
        [Route("GetProductsNodiscontinued")]
        public IEnumerable<object> GetProductsNodiscontinued()
        {
            var result =
                from c in contexto.Categories
                join p in contexto.Products on c.CategoryId equals p.CategoryId
                where p.Discontinued == false
                select new
                {
                    Nombre = p.ProductName,
                    Categoria = c.CategoryName,
                    Existencias = p.UnitsInStock
                };
            return result;
        }

        [HttpGet]
        [Route("GetProductsNodiscontinued2")]
        public IEnumerable<object> GetProductsNodiscontinued2()
        {
            var result = contexto.Products
                .Where(p => p.Discontinued == false)
                .Join(contexto.Categories,
                     (p) => p.CategoryId,
                     (c) => c.CategoryId,
                     (p, c) =>
                         new
                         {
                             Nombre = p.ProductName,
                             Categoria = c.CategoryName,
                             Existencias = p.UnitsInStock
                         }
                     );

            return result;

        }

        [HttpGet]
        [Route("Get5Products/{year}")]
        public IEnumerable<object> GetCincoProductos(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = startDate.AddYears(1).AddDays(-1);

            var result = new List<object>();

            for (int quarter = 1; quarter <= 4; quarter++)
            {
                var quarterlyResult = contexto.Products
                    .Join(contexto.Orderdetails,
                        p => p.ProductId,
                        od => od.ProductId,
                        (p, od) => new { Product = p, OrderDetail = od })
                    .Join(contexto.Orders,
                        join1 => join1.OrderDetail.OrderId,
                        o => o.OrderId,
                        (join1, o) => new { join1.Product, join1.OrderDetail, Order = o })
                    .Where(join2 => join2.Order.OrderDate.Value >= startDate &&
                                     join2.Order.OrderDate.Value <= endDate &&
                                     ((join2.Order.OrderDate.Value.Month - 1) / 3 + 1) == quarter)
                    .GroupBy(join2 => new { join2.Product.ProductName, Quarter = quarter })
                    .Select(group => new
                    {
                        Nombre = group.Key.ProductName,
                        Trimestre = group.Key.Quarter,
                        UnidadesVendidas = group.Sum(g => g.OrderDetail.Quantity)
                    })
                    .OrderByDescending(item => item.UnidadesVendidas)
                    .Take(5);

                result.AddRange(quarterlyResult);
            }

            return result;
        }

        [HttpGet]
        [Route("GetSalesOfProduct/{productName}/{startDateStr}/{endDateStr}")]
        public IEnumerable<object> GetProductSales(string productName, string startDateStr, string endDateStr)
        {
            DateTime startDate = DateTime.Parse(startDateStr);
            DateTime endDate = DateTime.Parse(endDateStr);

            var result = contexto.Orderdetails
                .Join(contexto.Orders,
                    od => od.OrderId,
                    o => o.OrderId,
                    (od, o) => new { od, o })
                .Join(contexto.Products,
                    od_o => od_o.od.ProductId,
                    p => p.ProductId,
                    (od_o, p) => new { od_o.od.Quantity, od_o.od.UnitPrice, od_o.o.OrderDate, p.ProductName })
                .Where(x => x.ProductName == productName && x.OrderDate >= startDate && x.OrderDate <= endDate)
                .GroupBy(x => new { x.ProductName, Year = x.OrderDate.Value.Year, Month = x.OrderDate.Value.Month })
                .Select(g => new
                {
                    g.Key.ProductName,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalSales = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();
            return result;
        }



        [HttpGet]
        [Route("GetSalesMonth/{startDate}/{endDate}")]
        public IEnumerable<object> GetPeriodo(string startDate, string endDate)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            var result = contexto.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .Join(contexto.Orderdetails,
                    order => order.OrderId,
                    orderDetail => orderDetail.OrderId,
                    (order, orderDetail) => new { order.OrderDate, orderDetail.UnitPrice, orderDetail.Quantity, orderDetail.ProductId })
                .Join(contexto.Products,
                    od => od.ProductId,
                    product => product.ProductId,
                    (od, product) => new { od.OrderDate, product.UnitPrice, od.Quantity })
                .GroupBy(o => new { Year = o.OrderDate.Value.Year, Month = o.OrderDate.Value.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, TotalPrice = g.Sum(o => o.UnitPrice * o.Quantity) })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            return result;
        }










        [HttpGet]
        [Route("GetNameAndPrice")]
        public IEnumerable<object> GetNameAndPrice()
        {
            IEnumerable<object> lista =
                from producto in contexto.Products
                select new
                {
                    Name = producto.ProductName,
                    Price = producto.UnitPrice,
                    Category = producto.Category.CategoryName
                };

            return lista;
        }

        [HttpGet]
        [Route("GetNameAndPrice2")]
        public IEnumerable<Product> GetNameAndPrice2()
        {
            Product p = new Product()
            {
                ProductName = "A",
                UnitPrice = 1
            };

            Product p1 = new Product();
            p1.ProductName = "A";
            p1.UnitPrice = 1;



            IEnumerable<Product> listaP =
                from producto in contexto.Products
                select new Product()
                {
                    ProductName = producto.ProductName,
                    UnitPrice = producto.UnitPrice
                };
            return listaP;
        }

        [HttpGet]
        [Route("GetInventarioCategoria")]
        public IEnumerable<object> GetInventarioCategoria()
        {
            IEnumerable<object> lista =
                contexto.Products.
                Join(contexto.Categories,
                (p) => p.CategoryId,
                (c) => c.CategoryId,
                (p, c) =>
                    new
                    {
                        Categoria = c.CategoryName,
                        Existencia = p.UnitPrice
                    }
                ).GroupBy(pc => pc.Categoria)
                .Select(grupo =>
                    new {
                        Categoria = grupo.Key,
                        Inventario = grupo.Sum(g => g.Existencia)
                    }
                );

            return lista;
        }



        [HttpGet]
        [Route("GetProductsIDName")]
        public IEnumerable<object> GetProductsIDName()
        {
            var result = from p in contexto.Products
                         select new
                         {
                             productId = p.ProductId,
                             productName = p.ProductName
                         };

            return result;
        }

        [HttpGet]
        [Route("GetCountry")]
        public IEnumerable<object> GetCountry()
        {
            var result = contexto.Customers
                .Select(c => c.Country)
                .Distinct()
                .Select(country => new { Country = country })
                .ToList();

            return result;
        }





        [HttpGet]
        [Route("GetTodo/{Pais}/{Fecha}")]
        public IActionResult GetTodo(string Pais, int Fecha)
        {
            var query = (from c in contexto.Customers
                         join o in contexto.Orders on c.CustomerId equals o.CustomerId
                         join od in contexto.Orderdetails on o.OrderId equals od.OrderId
                         join p in contexto.Products on od.ProductId equals p.ProductId
                         where c.Country == Pais && o.OrderDate.Value.Year == Fecha
                         orderby od.Quantity descending
                         select new
                         {
                             c.CustomerId,
                             c.CompanyName,
                             p.ProductName,
                             od.Quantity,
                             od.UnitPrice
                         });

            return Ok(query);
        }


        [HttpGet]
        [Route("GetCity")]
        public IEnumerable<object> GetCity()
        {
            var result = contexto.Customers
                .Select(c => c.City)
                .Distinct()
                .Select(city => new { City = city })
                .ToList();

            return result;
        }


        [HttpGet]
        [Route("GetOtro")]
        public IActionResult GetOtro()
        {
            var result = from customer in contexto.Customers
                         select new
                         {
                             customer.CompanyName,
                             customer.City,
                             TotalSales = (from orderDetail in contexto.Orderdetails
                                           join order in contexto.Orders on orderDetail.OrderId equals order.OrderId
                                           where order.CustomerId == customer.CustomerId
                                           select orderDetail.UnitPrice * orderDetail.Quantity).Sum()
                         };

            return Ok(result);
        }

        [HttpGet]
        [Route("GetCiudad/{ciudad}")]
        public IActionResult GetCiudad(string ciudad)
        {
            var result = from customer in contexto.Customers
                         where customer.City == ciudad
                         select new
                         {
                             customer.CompanyName,
                             customer.City,
                             TotalSales = (from orderDetail in contexto.Orderdetails
                                           join order in contexto.Orders on orderDetail.OrderId equals order.OrderId
                                           where order.CustomerId == customer.CustomerId
                                           select orderDetail.UnitPrice * orderDetail.Quantity).Sum()
                         };

            return Ok(result);
        }

        [HttpGet]
        [Route("GetCiudadAnio/{ciudad}/{anio}")]
        public IActionResult GetCiudadAnio(string ciudad, int anio)
        {
            var year = anio; // Reemplaza con el año específico deseado

            var query = from customer in contexto.Customers
                        where customer.City == ciudad
                        select new
                        {
                            customer.CompanyName,
                            customer.City,
                            TotalSales = (from orderDetail in contexto.Orderdetails
                                          join order in contexto.Orders on orderDetail.OrderId equals order.OrderId
                                          where order.CustomerId == customer.CustomerId
                                          && order.OrderDate.Value.Year == year
                                          select orderDetail.UnitPrice * orderDetail.Quantity)
                                         .Sum()
                        };

            return Ok(query.ToList());
        }

        [HttpGet]
        [Route("GetTodoo/{anio}")]
        public IActionResult GetTodoo(int anio)
        {

            var query = (from nueva in
                (from customer in contexto.Customers
                 select new
                 {
                     customer.CompanyName,
                     customer.City,
                     TotalSales = (from order in contexto.Orders
                                   join orderDetail in contexto.Orderdetails on order.OrderId equals orderDetail.OrderId
                                   where order.CustomerId == customer.CustomerId
                                   && order.OrderDate.Value.Year == anio
                                   select orderDetail.UnitPrice * orderDetail.Quantity).Sum()
                 })
                         where nueva.TotalSales > 0
                         select new
                         {
                             nueva.CompanyName,
                             nueva.City,
                             nueva.TotalSales
                         }
                        );
            return Ok(query);
        }




    }


}
