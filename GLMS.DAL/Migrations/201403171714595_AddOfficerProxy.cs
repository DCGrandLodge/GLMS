namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOfficerProxy : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LodgeOfficers", "Proxy", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LodgeOfficers", "Proxy");
        }
    }
}
