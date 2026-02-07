using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolSchedule.Entites
{
    [Table("Classrooms")]
    public class Classroom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Number { get; set; } // "101", "205А"

        public int Capacity { get; set; } // Вместимость (человек)

        [MaxLength(50)]
        public string Type { get; set; } // "Обычный", "Компьютерный", "Спортзал"

        // Переопределение метода ToString удобно для WPF ComboBox (будет показывать номер)
        public override string ToString() => $"{Number} ({Type})";
    }
}
