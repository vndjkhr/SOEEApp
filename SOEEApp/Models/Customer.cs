using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SOEEApp.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        [Required]
        public string CustomerName { get; set; }
    }
}