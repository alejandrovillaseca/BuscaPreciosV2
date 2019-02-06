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
        public long CantPaginas { get; set; }
        public Url()
        {
            this.Id = Guid.NewGuid().ToString();
        }
    }

    public class UrlResponse
    {
        public Header Header { get; set; }
        public List<Url> Urls { get; set; }
    }
}
