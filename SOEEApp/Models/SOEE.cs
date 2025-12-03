using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOEEApp.Models
{
    public enum SOEEStatus
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Dispatched = 3,
        PartiallyPaid = 4,
        Reconciled = 5,
        Closed = 6
    }

    public class SOEE
    {
        [Key]
        public int SOEEID { get; set; }

        [Required]
        public int ProjectID { get; set; }
        public virtual Project Project { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime SOEERaiseDate { get; set; } = DateTime.Now;
        public int CustomerID { get; set; }
        public virtual Customer Customer { get; set; }
        public string MarkTo { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string Reference { get; set; }
        public decimal TotalBasicAmount { get; set; }
        public decimal TotalServiceCharge { get; set; }
        public decimal TotalTaxAmount { get; internal set; }
        public decimal GrandTotal { get; set; }

        public decimal PreviousSOEEBalance { get; set; }
        public int? PrevSOEEID { get; set; }

        public SOEEStatus Status { get; set; } = SOEEStatus.Draft;
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ClosedDate { get; set; }

        public virtual ICollection<SOEEItem> Items { get; set; }
        public virtual ICollection<SOEEHistory> History { get; set; }
        public string CustomerName { get; internal set; }

        
    }

}
