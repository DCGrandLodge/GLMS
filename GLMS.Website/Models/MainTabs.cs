using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GLMS.BLL.Entities;
using GLMS.Models;

namespace GLMS.Website.Models
{
    public enum MainTab { Dashboard, Members, Lodges, Reports, Tools }
    public enum MemberTab { }
    public enum LodgeTab { }
    public enum ReportTab { }
    public enum ToolTab { Users, Configuration, Import }


    public static class Tabs
    {
        public static List<TabDefinition<MainTab>> MainTabs;
        public static List<TabDefinition<MemberTab>> MemberTabs;
        public static List<TabDefinition<LodgeTab>> LodgeTabs;
        public static List<TabDefinition<ReportTab>> ReportTabs;
        public static List<TabDefinition<ToolTab>> ToolTabs;

        static Tabs()
        {
            UrlHelper Url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            MainTabs = new List<TabDefinition<MainTab>>()
            {
                new TabDefinition<MainTab>(MainTab.Dashboard, "Dashboard", Url.Action("Index", "Home")),
                new TabDefinition<MainTab>(MainTab.Members, "Members", Url.Action("Index", "Member")),
                new TabDefinition<MainTab>(MainTab.Lodges, "Lodges", Url.Action("Index", "Lodge")),
                new TabDefinition<MainTab>(MainTab.Reports, "Reports", Url.Action("Index", "Reports")),
                new TabDefinition<MainTab>(MainTab.Tools, "Tools", Url.Action("Index", "Users")),
            };

            MemberTabs = new List<TabDefinition<MemberTab>>()
            {
            };

            LodgeTabs = new List<TabDefinition<LodgeTab>>()
            {
            };

            ReportTabs = new List<TabDefinition<ReportTab>>()
            {
            };

            ToolTabs = new List<TabDefinition<ToolTab>>()
            {
                new TabDefinition<ToolTab>(ToolTab.Users, "Users", null),
                new TabDefinition<ToolTab>(ToolTab.Configuration, "Configuration", Url.Action("Configuration","Tools")),
                new TabDefinition<ToolTab>(ToolTab.Import, "Import", Url.Action("Import","Tools")),
            };
        }


        public static IEnumerable<TabModel> GetTabModel(MainTab current)
        {
            return MainTabs.Where(x =>
                    (x.TabID == MainTab.Dashboard) ||
                    (x.TabID == MainTab.Members && CurrentUser.HasPermission(AccessLevel.Lodge)) ||
                    (x.TabID == MainTab.Lodges && CurrentUser.HasPermission(AccessLevel.System)) ||
                    (x.TabID == MainTab.Reports && CurrentUser.HasPermission(AccessLevel.System)) ||
                    (x.TabID == MainTab.Tools && CurrentUser.HasPermission(AccessLevel.System)))
                .Select(x => x.ToTabModel(current));
        }

        public static IEnumerable<TabModel> GetTabModel(LodgeTab current)
        {
            return LodgeTabs.Select(x => x.ToTabModel(current));
        }

        public static IEnumerable<TabModel> GetTabModel(MemberTab current)
        {
            return MemberTabs.Select(x => x.ToTabModel(current));
        }

        public static IEnumerable<TabModel> GetTabModel(ReportTab current)
        {
            return ReportTabs.Select(x => x.ToTabModel(current));
        }

        public static IEnumerable<TabModel> GetTabModel(ToolTab current)
        {
            return ToolTabs.Select(x => x.ToTabModel(current));
        }

    }
}