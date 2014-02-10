using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLMS.Models
{
    [Serializable]
    public class TabModel
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public bool Active { get; set; }
        public bool Disabled { get; set; }
        public IEnumerable<TabModel> Submenu { get; set; }
    }

    public class TabDefinition<T>
    {
        public T TabID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }

        public TabDefinition(T TabID, string Name, string Url)
        {
            this.TabID = TabID;
            this.Name = Name;
            this.Url = Url;
        }

        public TabModel ToTabModel(T activeTab, Guid? id = null)
        {
            return new TabModel()
            {
                Active = TabID.Equals(activeTab),
                Id = TabID.ToString(),
                Name = Name,
                Url = this.Url == null ? null : id.HasValue ? this.Url.Replace("_ID_", id.Value.ToString()) : this.Url.Replace("/_ID_", "")
            };
        }
    }
}
