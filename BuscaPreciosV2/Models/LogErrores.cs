using System;
using System.Collections.Generic;
using System.Text;

namespace BuscaPreciosV2.Models
{
    public class LogErrores
    {
        public string Id { get; private set; }
        public string Observaciones { get; set; }
        public string URL { get; set; }
        public LogErrores()
        {
            this.Id = Guid.NewGuid().ToString();
        }
    }
}
