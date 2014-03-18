namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLodgeMergeDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Lodge", "MergedDate", c => c.DateTime());
            DropColumn("dbo.Lodge", "Status");
            Sql("ALTER TABLE dbo.Lodge ADD Status as case when DarkDate is not null then 'Dark' when MergedWithLodgeID is not null then 'Merged' when CharterDate is not null then 'Active' when DispensationDate is not null then 'UD' else 'N/A' end");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Lodge", "MergedDate");
            DropColumn("dbo.Lodge", "Status");
            Sql("ALTER TABLE dbo.Lodge ADD Status as case when DarkDate is not null then 'Dark' when MergedWithLodgeID is not null then 'Merged' when CharterDate is not null then 'Chartered' when DispensationDate is not null then 'UD' else 'N/A' end");
        }
    }
}
