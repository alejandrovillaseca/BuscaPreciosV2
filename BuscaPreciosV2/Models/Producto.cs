using System;

namespace BuscaPreciosV2.Models
{
    internal class Producto : Header
    {
        public int id { get; set; }
        public string NombreProducto { get; set; }
        public string CodigoProcudto { get; set; }
        public string Marca { get; set; }

        public int? Precio { get; set; }

        public int? PrecioInternet { get; set; }

        public int? PrecioNormal { get; set; }

        public string Link { get; set; }
        public int? idURL { get; set; }

        public bool? DescuentoCMR { get; set; }

        public string Observaciones { get; set; }

        public DateTime FechaProceso { get; set; }
        public bool? Correcto { get; set; }
        public int idProceso { get; set; }
    }
}