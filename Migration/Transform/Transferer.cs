using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupportSiteETL.Migration.Load;

namespace SupportSiteETL.Migration.Transform
{
    //the top level transferer class, organizes the flow of info between the posttransferer and user transferer
    public class Transferer
    {
        UserTransferer ut;
        CategoryTransferer ct;
        PostTransferer pt;
        Loader loader;

        public Transferer()
        {
            ut = new UserTransferer();
            ct = new CategoryTransferer();
            pt = new PostTransferer();
            loader = new Loader();
        }

        public void Extract()
        {
            ut.Extract();
            ct.Extract();
            pt.Extract(ut.oldToNewId, ct.catIdMap); //the post extraction requires the user id conversion dictionary
        }

        public void Load()
        {
            ut.Load();
            ct.Load();
            pt.Load();
            ct.updateCategoryCounts(); //update the category count now that post data is loaded
            loader.UpdateSiteStats();

        }
    }
}
