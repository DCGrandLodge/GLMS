using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.BLL
{
    // Field Length constants to make it easier to keep UI model and DB schema in sync
    public static class FieldLengths
    {
        public static class Lodge
        {
            public const int Name = 120;
            public const int Phone = 20;
            public const int MeetingDate = 64;
        }

        public static class Address
        {
            public const int Street = 128;
            public const int City = 128;
            public const int State = 2;
            public const int Zip = 10;
            public const int Country = 128;
        }

        public static class Degree
        {
            public const int Name = 120;
            public const int Abbr = 16;
        }

        public static class Member
        {
            public const int FirstName = 120;
            public const int MiddleName = 120;
            public const int LastName = 120;
        }

        public static class Office
        {
            public const int Title = 120;
            public const int Abbr = 16;
        }

        public static class User
        {
            public const int Username = 64;
            public const int FirstName = 120;
            public const int LastName = 120;
        }

        public static class ZipCode
        {
            public const int Zip = 10;
            public const int City = 120;
            public const int State = 120;
            public const int StateAbbr = 2;
        }

    }
}
