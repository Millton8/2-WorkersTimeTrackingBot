using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kpworkersbot
{
    internal class UserInfo
    {
        public DateTime tBegin;
        public string project;
        public UserInfo()
        {

        }
        public UserInfo(string projects)
        {
            tBegin = DateTime.Now;
            this.project = projects;
        }
    }
}
