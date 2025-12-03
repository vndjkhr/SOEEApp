using System;
using System.ComponentModel.DataAnnotations;

namespace SOEEApp.Models
{
    public class SOEEHistory
    {
        [Key]
        public int Id { get; set; }
        public int SOEEID { get; set; }
        public virtual SOEE SOEE { get; set; }
        public string ActionBy { get; set; }
        public DateTime ActionOn { get; set; } = DateTime.Now;
        public string Action { get; set; }   // e.g., "Submitted", "Approved", "E-Signed"
        public string Notes { get; set; }
    }
}
