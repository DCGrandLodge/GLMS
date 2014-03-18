using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using GLMS.BLL;
using GLMS.MVC.Extensions.jqGrid;

namespace GLMS.Website.Models
{
    #region Index
    public class LodgeIndexModel : GridModel
    {
        public class Data
        {
            public Guid LodgeID { get; set; }
            public int Number { get; set; }
            public string Name { get; set; }
            public string PhoneNumber { get; set; }
            public string Address { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
        }

        public static string GridKey = "LodgeIndex";
        public LodgeIndexModel() : base("lodge-index", GridKey)
        {

        }
    }
    #endregion

    public class LodgeViewModel
    {
        public Guid LodgeID { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Status { get; set; }
        public DateTime? StatusDate { get; set; }

        public DateTime? CharterDate { get; set; }
        public DateTime? DarkDate { get; set; }
        public DateTime? DispensationDate { get; set; }
        public string MeetingDates { get; set; }

        public LodgeViewModel MergedInto { get; set; }
        public IEnumerable<LodgeViewModel> MergedLodges { get; set; }

        public IEnumerable<LodgeOfficerModel> Officers { get; set; }


        public IEnumerable<LodgeOfficerModel> HonoraryOfficers { get; set; }
    }

    public class LodgeEditModel
    {
        [Required]
        public Guid LodgeID { get; set; }

        [Required, StringLength(FieldLengths.Lodge.Name)]
        public string Name { get; set; }
        [Required]
        public int Number { get; set; }

        public AddressEditModel Address { get; set; }

        [Display(Name="Phone Number"), StringLength(FieldLengths.Lodge.Phone)]
        public string PhoneNumber { get; set; }

        [Display(Name="Meeting Dates"), StringLength(FieldLengths.Lodge.MeetingDate)]
        public string MeetingDates { get; set; }

        public List<LodgeOfficerModel> Officers { get; set; }
    }

    public class LodgeOfficerModel
    {
        public Guid OfficeID { get; set; }
        public string Title { get; set; }
        [Display(Name = "Officer Name")]
        public Guid? MemberID { get; set; }
        [Display(Name = "Officer Name")]
        public string Name { get; set; }
        public bool Proxy { get; set; }
    }


}