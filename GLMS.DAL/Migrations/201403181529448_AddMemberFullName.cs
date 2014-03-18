namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMemberFullName : DbMigration
    {
        public override void Up()
        {
            Sql("ALTER TABLE dbo.Member ADD FullName as rtrim(IsNull(LastName,'') + ', ' + IsNull(FirstName,'') + ' ' + IsNull(MiddleName,''))");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Member", "FullName");
        }
    }
}
