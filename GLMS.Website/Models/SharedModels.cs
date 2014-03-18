using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using GLMS.BLL;

namespace GLMS.Website.Models
{
    public class AddressEditModel
    {
        [Display(Name = "Street Address"), StringLength(FieldLengths.Address.Street)]
        public string Street { get; set; }
        [StringLength(FieldLengths.Address.City)]
        public string City { get; set; }
        [StringLength(FieldLengths.Address.State)]
        public string State { get; set; }
        [Display(Name = "Zip Code"), StringLength(FieldLengths.Address.Zip)]
        public string Zip { get; set; }
        [StringLength(FieldLengths.Address.Country)]
        public string Country { get; set; }
    }
}