using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Migration.Transform.Models
{
    public class Q2ACategory
    {
        public int id; //id of category
        public int? parentid; //parent id of category, for nesting, null if no parent
        public string title; //category name
        public string tag; //tag of category, should be the same as backpath
        public string backpath; //path of category, lowercase with no space version of title (hyphen instead of space)
        public string content; //category description
        public int qcount; //number of question under category
        public int position; //order of category, 1,2, or 3, etc.

        public Q2ACategory()
        {
            id = 0;
            parentid = null;
            title = "";
            tag = "";
            backpath = "";
            content = "";

            qcount = 0;
            position = 0;
        }

        //from the title, set the tag and backpath, i.e. "Test Category 1" turns into "test-category-1", assumes title is already set
        public void GenBackpath()
        {
            backpath = "";
            foreach(char c in title)
            {
                if (c == ' ') //space
                    backpath = backpath + '-'; //replace with hyphen
                else if (char.IsLetter(c)) //letters should be lowercase
                    backpath = backpath + char.ToLower(c);
                else //other characters should be fine
                    backpath = backpath + c;
            }
            tag = backpath; //tag should be the same as backpath
        }
    }
}
