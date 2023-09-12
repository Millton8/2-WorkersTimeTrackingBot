using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kpworkersbot
{
    internal class WorkRezult
    {
        public string ID;
        public string? name;
        public UserInfo beginAndProject;
        public DateTime tEnd;
        public int pricePerHour;
        public float salary;

        public WorkRezult(string ID,string name, UserInfo beginAndProject, int pricePerHour)
        {
            this.ID = ID;
            this.name = name ?? "Нет имени";
            this.beginAndProject = beginAndProject;
            this.tEnd = DateTime.Now;
            this.pricePerHour = pricePerHour;
            this.salary = pricePerHour * Convert.ToSingle((DateTime.Now - beginAndProject.tBegin).TotalHours);
        }

    }
}
