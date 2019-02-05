using System;

namespace BuscaPreciosV2.Models.Sodimac
{

    public partial class Producto
    {
        public int id { get; set; }
        public string NombreProducto { get; set; }
        public string Marca { get; set; }
        public Nullable<int> Precio { get; set; }
        public Nullable<int> PrecioInternet { get; set; }
        public Nullable<int> PrecioNormal { get; set; }
        public string Link { get; set; }
        public Nullable<bool> DescuentoCMR { get; set; }
        public string Observaciones { get; set; }
        public Nullable<DateTime> FechaProceso { get; set; }
    }
}