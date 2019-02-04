using System;
using System.Collections.Generic;
using System.Text;

namespace BuscaPreciosV2.Models
{
    public class LogErrores
    {
        public string id { get; set; }
        public string Observaciones { get; set; }
        public string URL { get; set; }
        public int idProceso { get; set; }
        public int idSistema { get; set; }
    }
}
