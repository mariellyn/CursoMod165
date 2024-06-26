﻿using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CursoMod165.Models
{
    public class Appointment
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Appointment #")]
        public string Number { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Time")]
        public DateTime Time { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Informations")]
        public string Informations { get; set; } = "";

        [Required]
        [Display(Name = "Is Done?")]
        public bool IsDone { get; set; }


        [ForeignKey("StaffID")]
        [ValidateNever]
        public Staff Staff { get; set; }

        [Display(Name = "Staff")]
        [Required]
        public int StaffID { get; set; }

        [ForeignKey("CustomerID")]
        [ValidateNever]
        public Customer Customer { get; set; }

        [Display(Name = "Customer")]
        [Required]
        public int CustomerID { get; set; }
    }
}
