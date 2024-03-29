﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace LogicUniversity.Models
{
    public class RequisitionDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequisitionDetailsId { get; set; }
        public int RequisitionId { get; set; }
        public string ItemCode { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }

        [ForeignKey("RequisitionId")]
        public virtual Requisition Requisition { get; set; }
        public  virtual Products Products { get; set; }
        
    }
}