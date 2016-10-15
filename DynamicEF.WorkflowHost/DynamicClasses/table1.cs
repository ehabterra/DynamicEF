using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicEF.DAL
{
    public class table1
    {
        public int Id { get; set; }

        [Required()]
        public string Title { get; set; }
    }
}
