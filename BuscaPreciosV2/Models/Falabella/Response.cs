using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuscaPreciosV2.Models.Falabella
{
    public class Response
    {
        public Header Header { get; set; }
        public List<ProductoResponse> Productos { get; set; }
    }

    public class ResponsePorPagina
    {
        public Header Header { get; set; }
        public ProductoResponse ProductosPorPágina { get; set; }
    }
}
