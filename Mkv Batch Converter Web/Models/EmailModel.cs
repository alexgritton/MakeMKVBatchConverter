using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Mkv_Batch_Converter_Web.Models
{
    public class EmailModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Comment { get; set; }

        public EmailModel()
        {
            Name = string.Empty;
            Email = string.Empty;
            Comment = string.Empty;
        }
    }
}