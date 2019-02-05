using System;
using System.Collections.Generic;

namespace BuscaPreciosV2.Models.Falabella
{
    public partial class URL
    {
        public int id { get; set; }
        public string url { get; set; }
        public Nullable<int> CantPaginas { get; set; }
        public bool Activo { get; set; }
        public bool Correcto { get; set; }
        public System.DateTime FechaStatus { get; set; }
        public string Data { get; set; }
        public string Observaciones { get; set; }
    }
}