using System;

namespace BuscaPreciosV2.Models
{
    public class Stats
    {
        public string Id { get; set; }
        public DateTime FechaInicio { get; private set; }
        public DateTime? FechaFin { get; set; }
        public TimeSpan? Duracion { get; set; }
        public int? CantidadProductos { get; set; }
        public bool? Cargado { get; set; }
        public string Observacion { get; set; }
        public int? IdURL { get; set; }
        public int Sistema { get; set; }
    }
}
