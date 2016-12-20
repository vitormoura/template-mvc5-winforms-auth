using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LabAutenticacao.Models
{
    public class FormLogin
    {
        public String LoginURL { get; set; }
        
        [Required]
        public String Usuario { get; set; }

        [Required]
        public String Senha { get; set; }

        public String Mensagem { get; set; }
    }
}