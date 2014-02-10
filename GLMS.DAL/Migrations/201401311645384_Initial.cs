namespace GLMS.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Degree",
                c => new
                    {
                        DegreeID = c.Guid(nullable: false),
                        Number = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 120),
                        Abbv = c.String(nullable: false, maxLength: 16),
                    })
                .PrimaryKey(t => t.DegreeID);
            
            CreateTable(
                "dbo.Lodge",
                c => new
                    {
                        LodgeID = c.Guid(nullable: false),
                        Number = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 120),
                        Address_Street = c.String(maxLength: 128),
                        Address_City = c.String(maxLength: 128),
                        Address_State = c.String(maxLength: 2),
                        Address_Zip = c.String(maxLength: 10),
                        Address_Country = c.String(maxLength: 128),
                        PhoneNumber = c.String(maxLength: 20),
                        MeetingDates = c.String(maxLength: 64),
                        DuesPaying = c.Boolean(nullable: false),
                        DispensationDate = c.DateTime(),
                        CharterDate = c.DateTime(),
                        DarkDate = c.DateTime(),
                        MergedWithLodgeID = c.Guid(),
                    })
                .PrimaryKey(t => t.LodgeID)
                .ForeignKey("dbo.Lodge", t => t.MergedWithLodgeID)
                .Index(t => t.MergedWithLodgeID);
            
            CreateTable(
                "dbo.LodgeMembership",
                c => new
                    {
                        LodgeID = c.Guid(nullable: false),
                        MemberID = c.Guid(nullable: false),
                        Status = c.Int(nullable: false),
                        PetitionType = c.Int(),
                        PetitionDate = c.DateTime(),
                        ElectedDate = c.DateTime(),
                        RejectedDate = c.DateTime(),
                        AffiliatedDate = c.DateTime(),
                        HonoraryDate = c.DateTime(),
                        DemitDate = c.DateTime(),
                        WithdrawDate = c.DateTime(),
                        NPDDate = c.DateTime(),
                        ExpelledDate = c.DateTime(),
                        ReinstatedDate = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.LodgeID, t.MemberID })
                .ForeignKey("dbo.Lodge", t => t.LodgeID, cascadeDelete: true)
                .ForeignKey("dbo.Member", t => t.MemberID, cascadeDelete: true)
                .Index(t => t.LodgeID)
                .Index(t => t.MemberID);
            
            CreateTable(
                "dbo.Member",
                c => new
                    {
                        MemberID = c.Guid(nullable: false),
                        FirstName = c.String(maxLength: 120),
                        MiddleName = c.String(maxLength: 120),
                        LastName = c.String(maxLength: 120),
                        DOB = c.DateTime(nullable: false),
                        DOD = c.DateTime(),
                        Clergy = c.Boolean(nullable: false),
                        Status = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MemberID);
            
            CreateTable(
                "dbo.MemberDegree",
                c => new
                    {
                        MemberID = c.Guid(nullable: false),
                        DegreeID = c.Guid(nullable: false),
                        Date = c.DateTime(),
                        LodgeID = c.Guid(),
                    })
                .PrimaryKey(t => new { t.MemberID, t.DegreeID })
                .ForeignKey("dbo.Degree", t => t.DegreeID, cascadeDelete: true)
                .ForeignKey("dbo.Lodge", t => t.LodgeID)
                .ForeignKey("dbo.Member", t => t.MemberID, cascadeDelete: true)
                .Index(t => t.DegreeID)
                .Index(t => t.LodgeID)
                .Index(t => t.MemberID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserID = c.Guid(nullable: false),
                        Username = c.String(nullable: false, maxLength: 64),
                        FirstName = c.String(nullable: false, maxLength: 120),
                        LastName = c.String(nullable: false, maxLength: 120),
                        Email = c.String(),
                        Active = c.Boolean(nullable: false),
                        Password_Encrypted = c.Binary(),
                        Password_Temporary = c.Binary(),
                        Password_TempExpiration = c.DateTime(),
                        Password_ForceChange = c.Boolean(nullable: false),
                        DateCreated = c.DateTime(),
                        LastLogin = c.DateTime(),
                        MemberID = c.Guid(),
                        AccessLevel = c.Int(nullable: false),
                        Member_MemberID = c.Guid(),
                    })
                .PrimaryKey(t => t.UserID)
                .ForeignKey("dbo.Member", t => t.Member_MemberID)
                .Index(t => t.Member_MemberID);
            
            CreateTable(
                "dbo.LodgeOfficers",
                c => new
                    {
                        LodgeID = c.Guid(nullable: false),
                        MemberID = c.Guid(nullable: false),
                        LodgeOfficerID = c.Guid(nullable: false),
                        LodgeOfficeID = c.Guid(nullable: false),
                        Appointed = c.Boolean(nullable: false),
                        DateElected = c.DateTime(nullable: false),
                        DateInstalled = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.LodgeID, t.MemberID })
                .ForeignKey("dbo.Lodge", t => t.LodgeID, cascadeDelete: true)
                .ForeignKey("dbo.Office", t => t.LodgeOfficeID, cascadeDelete: true)
                .ForeignKey("dbo.Member", t => t.MemberID, cascadeDelete: true)
                .Index(t => t.LodgeID)
                .Index(t => t.LodgeOfficeID)
                .Index(t => t.MemberID);
            
            CreateTable(
                "dbo.Office",
                c => new
                    {
                        OfficeID = c.Guid(nullable: false),
                        Title = c.String(maxLength: 120),
                        Abbr = c.String(maxLength: 16),
                    })
                .PrimaryKey(t => t.OfficeID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LodgeOfficers", "MemberID", "dbo.Member");
            DropForeignKey("dbo.LodgeOfficers", "LodgeOfficeID", "dbo.Office");
            DropForeignKey("dbo.LodgeOfficers", "LodgeID", "dbo.Lodge");
            DropForeignKey("dbo.Lodge", "MergedWithLodgeID", "dbo.Lodge");
            DropForeignKey("dbo.LodgeMembership", "MemberID", "dbo.Member");
            DropForeignKey("dbo.Users", "Member_MemberID", "dbo.Member");
            DropForeignKey("dbo.MemberDegree", "MemberID", "dbo.Member");
            DropForeignKey("dbo.MemberDegree", "LodgeID", "dbo.Lodge");
            DropForeignKey("dbo.MemberDegree", "DegreeID", "dbo.Degree");
            DropForeignKey("dbo.LodgeMembership", "LodgeID", "dbo.Lodge");
            DropIndex("dbo.LodgeOfficers", new[] { "MemberID" });
            DropIndex("dbo.LodgeOfficers", new[] { "LodgeOfficeID" });
            DropIndex("dbo.LodgeOfficers", new[] { "LodgeID" });
            DropIndex("dbo.Lodge", new[] { "MergedWithLodgeID" });
            DropIndex("dbo.LodgeMembership", new[] { "MemberID" });
            DropIndex("dbo.Users", new[] { "Member_MemberID" });
            DropIndex("dbo.MemberDegree", new[] { "MemberID" });
            DropIndex("dbo.MemberDegree", new[] { "LodgeID" });
            DropIndex("dbo.MemberDegree", new[] { "DegreeID" });
            DropIndex("dbo.LodgeMembership", new[] { "LodgeID" });
            DropTable("dbo.Office");
            DropTable("dbo.LodgeOfficers");
            DropTable("dbo.Users");
            DropTable("dbo.MemberDegree");
            DropTable("dbo.Member");
            DropTable("dbo.LodgeMembership");
            DropTable("dbo.Lodge");
            DropTable("dbo.Degree");
        }
    }
}
