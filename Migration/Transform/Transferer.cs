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
        PostTransferer pt;
        Loader loader;

        public Transferer()
        {
            ut = new UserTransferer();
            pt = new PostTransferer();
            loader = new Loader();
        }

        public void Extract()
        {
            ut.Extract();
            pt.Extract(ut.oldToNewId); //the post extraction requires the user id conversion dictionary
        }

        public void Load()
        {
            ut.Load();
            pt.Load();
            loader.UpdateSiteStats();

        }
    }
}
