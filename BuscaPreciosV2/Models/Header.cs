using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuscaPreciosV2.Models
{
    public class Header
    {
        public bool Correcto { get; set; }
        public DateTime FechaProceso { get; set; }
        public string Observación { get; set; }
    }
}
