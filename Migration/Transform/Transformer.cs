using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupportSiteETL.Migration.Load;

namespace SupportSiteETL.Migration.Transform
{
    //the top level transferer class, organizes the flow of info between the posttransferer and user transferer
    public class Transformer
    {
        UserTransformer ut;
        CategoryTransformer ct;
        PostTransformer pt;
        WordsTransformer wt;
        Loader loader;

        Anonymizer anon;

        public Transformer()
        {
            ut = new UserTransformer();
            ct = new CategoryTransformer();
            pt = new PostTransformer();
            wt = new WordsTransformer();
            loader = new Loader();

            anon = new Anonymizer();
        }

        public void Extract()
        {
            ut.Extract();

            //pass some key information to the different transformers
            anon.userIdMap = ut.oldToNewId;
            anon.q2aUsers = ut.newUsers;
            pt.anonymizer = anon;

            ct.Extract();

            //pass some key information to the different transformers
            pt.oldToNewId = ut.oldToNewId;
            pt.oldToNewCatId = ct.catIdMap;
            pt.devUserIds = ut.devUsers;

            pt.Extract(); //the post extraction information from the other classes
        }

        public void Load()
        {
            ut.Load(); //add the users to q2a
            ct.Load(); //add categories to q2a
            pt.Load(); //add posts to q2a

            ct.updateCategoryCounts(); //update the category count now that post data is loaded
            wt.Load(); //update all the word tables
            loader.UpdateSiteStats(); //update some key values in qa_stats

        }
    }
}
