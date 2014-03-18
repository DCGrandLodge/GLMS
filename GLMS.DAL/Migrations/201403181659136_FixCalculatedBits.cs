namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixCalculatedBits : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Lodge", "Merged");
            DropColumn("dbo.Lodge", "Dark");
            DropColumn("dbo.Lodge", "Chartered");
            DropColumn("dbo.Lodge", "UnderDispensation");
            DropColumn("dbo.Lodge", "Status");
            Sql("ALTER TABLE dbo.Lodge ADD Dark as cast(case when DarkDate is null then 0 else 1 end as bit)");
            Sql("ALTER TABLE dbo.Lodge ADD Merged as cast(case when MergedWithLodgeID is null then 0 else 1 end as bit)");
            Sql("ALTER TABLE dbo.Lodge ADD Chartered as cast(case when CharterDate is null or DarkDate is not null or MergedWithLodgeID is not null then 0 else 1 end as bit)");
            Sql("ALTER TABLE dbo.Lodge ADD UnderDispensation as cast(case when DispensationDate is null or CharterDate is not null or DarkDate is not null or MergedWithLodgeID is not null then 0 else 1 end as bit)");
            Sql("ALTER TABLE dbo.Lodge ADD Status as case when DarkDate is not null then 'Dark' when MergedWithLodgeID is not null then 'Merged' when CharterDate is not null then 'Chartered' when DispensationDate is not null then 'UD' else 'N/A' end");
        }
        
        public override void Down()
        {
        }
    }
}
