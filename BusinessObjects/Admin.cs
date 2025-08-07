using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Admin : BaseEntity
    {
        [Key, ForeignKey(nameof(User))]
        public Guid Id { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
