using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolSchedule.Entites
{
    [Table("Teachers")]
    public class Teacher
    {
        [Key]
        public int Id { get; set; }
        public int? SubjectId { get; set; }
        public virtual Subject? Subject { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } 
        public virtual ICollection<Workload> Workloads { get; set; } = new List<Workload>();
    }
}
