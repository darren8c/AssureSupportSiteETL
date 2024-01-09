using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupportSiteETL.Migration.Load;
using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Transform.Models;

namespace SupportSiteETL.Migration.Transform
{
    //creates and populates the qa_accountreclaim table
    //fields: userid (q2a id), email (discourse email)
    //there is no extract process for the account reclaiming only a load process
    public class AccountReclaimer
    {
        public List<Q2AUser> users; //users, set by transformer after userTransformer extracts
        private Loader loader;

        public AccountReclaimer()
        {
            loader = new Loader();
            //users set by transformer
        }

        //store the site on q2a
        public void Load()
        {
            //create the and write the rows
            //loader.AddAccountReclaimTable();
            loader.AddAccountReclaimData(users);
        }
    }
}
