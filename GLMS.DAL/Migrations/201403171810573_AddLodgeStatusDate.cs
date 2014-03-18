namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLodgeStatusDate : DbMigration
    {
        public override void Up()
        {
            Sql("ALTER TABLE dbo.Lodge ADD StatusDate as IsNull(IsNull(DarkDate,CharterDate),DispensationDate)");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Lodge", "StatusDate");
        }
    }
}
