using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace GLMS.MVC.Extensions.Back
{
    public static class BackTrail
    {
        private static Stack<string> Trail = new Stack<string>();

        public static string Back()
        {
            if (Trail.Count > 0)
            {
                return Trail.Pop();
            }
            else
            {
                return null;
            }
        }

        public static string Peek()
        {
            return Trail.Peek();
        }

        public static void Clear()
        {
            Trail.Clear();
        }

        public static void Push(string url)
        {
            if (Trail.Count == 0 || Trail.Peek() != url)
            {
                Trail.Push(url);
            }
        }
    }
}
