using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using GLMS.BLL;
using GLMS.BLL.Entities;

namespace GLMS.Migration
{
    public static class LegacyMigration
    {
        public static void MigrateAll(IGLMSContext context)
        {
            using (var connection = new OleDbConnection(ConfigurationManager.ConnectionStrings["Legacy"].ConnectionString))
            {
                connection.Open();
                MigrateLodges(connection, context);
            }
            context.SaveChanges();
        }

        private static void MigrateLodges(OleDbConnection legacy, IGLMSContext context)
        {
            var lodges = context.Lodges.ToList();
            using (var cmd = legacy.CreateCommand())
            {
                cmd.CommandText = "select * from lodges";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int lodgeNo = int.Parse(reader["lodge_no"].ToString());
                        if (lodgeNo == 0 || lodges.Any(x => x.Number == lodgeNo))
                        {
                            continue;
                        }
                        Lodge lodge = new Lodge()
                        {
                            LodgeID = Guid.NewGuid(),
                            Number = lodgeNo,
                            Name = reader["lodg_name"].ToString().Trim(),
                            Address = new Address()
                            {
                                Street = reader["street"].ToString().Trim(),
                                City = reader["city"].ToString().Trim(),
                                State = reader["state"].ToString().Trim(),
                                Zip = reader["zip"].ToString().Trim(),
                                Country = "United States",
                            },
                            PhoneNumber = reader["phone"].ToString().Trim(),
                            MeetingDates = reader["meetings"].ToString().Trim(),
                            DarkDate = ((bool)reader["dark"]) ? CheckDate(reader["darkdate"].ToString()) ?? DateTime.Now : (DateTime?)null,
                            CharterDate = CheckDate(reader["chrtr_date"].ToString()),
                        };
                        context.Add(lodge);
                        lodges.Add(lodge);
                    }
                }
            }
        }

        private static DateTime? CheckDate(string strDate)
        {
            DateTime date = DateTime.Parse(strDate.Trim());
            if (date.Date == new DateTime(1899, 12, 30))
            {
                return null;
            }
            return date;
        }
    }
}
