namespace BuscaPreciosV2.Models
{
    using System;

    public partial class SodimacCLURL
    {
        public int id { get; set; }
        public string URL { get; set; }
        public Nullable<int> CantPaginas { get; set; }
        public bool Activo { get; set; }
        public bool Correcto { get; set; }
        public DateTime FechaStatus { get; set; }
        public string Data { get; set; }
        public string Observaciones { get; set; }
    }
}