using System;
using System.Collections.Generic;

namespace BuscaPreciosV2.Models
{
    public partial class FalabellaURL
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FalabellaURL()
        {
            this.FalabellaProducto = new HashSet<FalabellaProducto>();
            this.Stats = new HashSet<Stats>();
        }

        public int id { get; set; }
        public string URL { get; set; }
        public Nullable<int> CantPaginas { get; set; }
        public bool Activo { get; set; }
        public bool Correcto { get; set; }
        public System.DateTime FechaStatus { get; set; }
        public string Data { get; set; }
        public string Observaciones { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<FalabellaProducto> FalabellaProducto { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Stats> Stats { get; set; }
    }
}