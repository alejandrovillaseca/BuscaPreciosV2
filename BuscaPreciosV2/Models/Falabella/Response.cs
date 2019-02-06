using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuscaPreciosV2.Models.Falabella
{
    /// <summary>
    /// Contiene todas las páginas de la categoría de productos
    /// </summary>
    public class Response
    {
        public Header Header { get; set; }
        public List<ResultList> Productos { get; set; }
        public ProductoResponse FullObject { get; set; }
    }
}
