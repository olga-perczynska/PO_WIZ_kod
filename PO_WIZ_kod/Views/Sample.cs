using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PO_WIZ_kod.Models
{
    public class Sample
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime? CollectionDate { get; set; }
        public string Notes { get; set; }

        public override string ToString()
        {
            return $"{Id}, {Name}, {Type}, {(CollectionDate.HasValue ? CollectionDate.Value.ToString("yyyy-MM-dd") : "")}, {Notes}";
        }
    }


}
