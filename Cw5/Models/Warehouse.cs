using System;
using System.ComponentModel.DataAnnotations;

namespace Cw5
{
    public class Warehouse
    {
        public int IdProduct { get; set; }
        [Required(ErrorMessage = "IdWareHouse required")]
        public int IdWarehouse { get; set; }
        [Required(ErrorMessage = "Amount required")]
        public int Amount { get; set; }
        [Required(ErrorMessage = "CreatedAt required")]
        public DateTime CreatedAt {get; set;}
        public int IdOrder { get; set; }
    }
}
