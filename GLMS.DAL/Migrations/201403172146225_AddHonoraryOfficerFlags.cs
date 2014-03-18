namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddHonoraryOfficerFlags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LodgeOfficers", "Honorary", c => c.Boolean(nullable: false));
            AddColumn("dbo.LodgeOfficers", "PastOfficer", c => c.Boolean(nullable: false));
            AddColumn("dbo.LodgeOfficers", "Emeritus", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LodgeOfficers", "Emeritus");
            DropColumn("dbo.LodgeOfficers", "PastOfficer");
            DropColumn("dbo.LodgeOfficers", "Honorary");
        }
    }
}
