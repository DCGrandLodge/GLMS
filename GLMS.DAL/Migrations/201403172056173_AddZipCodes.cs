namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddZipCodes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ZipCode",
                c => new
                    {
                        Zip = c.String(nullable: false, maxLength: 10),
                        City = c.String(nullable: false, maxLength: 120),
                        State = c.String(nullable: false, maxLength: 120),
                        StateAbbr = c.String(nullable: false, maxLength: 2),
                    })
                .PrimaryKey(t => t.Zip);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ZipCode");
        }
    }
}
