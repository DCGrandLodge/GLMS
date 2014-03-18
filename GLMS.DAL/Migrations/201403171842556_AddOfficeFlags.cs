namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOfficeFlags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Office", "GrandOffice", c => c.Boolean(nullable: false));
            AddColumn("dbo.Office", "Sequence", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Office", "Sequence");
            DropColumn("dbo.Office", "GrandOffice");
        }
    }
}
