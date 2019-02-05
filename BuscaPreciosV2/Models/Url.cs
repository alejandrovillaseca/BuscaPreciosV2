using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuscaPreciosV2.Models
{
    public class Url
    {
        public string Id { get; private set; }
        public string URL { get; set; }
        public Url()
        {
            this.Id = Guid.NewGuid().ToString();
        }
    }
}
