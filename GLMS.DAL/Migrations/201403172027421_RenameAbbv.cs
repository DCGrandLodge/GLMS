namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameAbbv : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.Degree", "Abbv", "Abbr");
        }

        public override void Down()
        {
            RenameColumn("dbo.Degree", "Abbr", "Abbv");
        }
    }
}
