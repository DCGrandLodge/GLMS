using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GLMS.BLL.Entities;
using LINQtoCSV;

namespace GLMS.DAL.Migrations.Seed
{
    public static class ZipCodeData
    {
        public static void Seed(GLMSContext context)
        {
            if (!context.ZipCodes.Any())
            {
                Assembly assembly = typeof(Configuration).Assembly;
                string filename = String.Format("{0}.ZipCodes.csv", assembly.GetName().Name);
                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(filename))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            CsvContext csv = new CsvContext();
                            IEnumerable<ZipCode> sourceData = csv.Read<ZipCode>(reader,
                                new CsvFileDescription()
                                {
                                    FirstLineHasColumnNames = true,
                                    EnforceCsvColumnAttribute = false,
                                    SeparatorChar = '\t',
                                });
                            /*
                            context.BulkInsert<ZipCode>(sourceData.Select(x => new ZipCode()
                            {
                                Zip = x.ZipCode,
                                City = x.City,
                                State = x.State,
                                StateAbbr = x.StateAbbr
                            }));
                             */
                            context.BulkInsert<ZipCode>(sourceData.ToList());
                        }
                    }
                }
                catch (AggregatedException ex)
                {
                    throw new Exception(String.Join(", ", ex.m_InnerExceptionsList.Select(y => y.Message)));
                }
                catch (LINQtoCSVException ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        private class ZipCodeCSV
        {
            public string ZipCode { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string StateAbbr { get; set; }
        }
    }
}
