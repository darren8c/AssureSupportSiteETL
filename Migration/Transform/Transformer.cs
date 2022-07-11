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
        ImageTransformer it;
        AccountReclaimer ar;
        AutoTagger at;

        Loader loader;

        Anonymizer anon;

        public Transformer()
        {
            ut = new UserTransformer();
            ct = new CategoryTransformer();
            pt = new PostTransformer();
            wt = new WordsTransformer();
            it = new ImageTransformer();
            ar = new AccountReclaimer();
            at = new AutoTagger();

            loader = new Loader();

            anon = new Anonymizer();
        }

        public void Extract()
        {
            ut.Extract();
            ar.users = ut.newUsers; //account reclaim requires the list of new users

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
            at.Extract(ref pt.allPosts); //assign the posts tags
            it.Extract(ref pt.allPosts); //change all the images in the posts, changes will be reflected in the post transferer
        }

        public void Load()
        {
            ut.Load(); //add the users to q2a
            ar.Load(); //add and fill the account reclaim table
            
            ct.Load(); //add categories to q2a
            pt.Load(); //add posts to q2a
            it.Load(); //add images to q2a

            ct.updateCategoryCounts(); //update the category count now that post data is loaded
            wt.Load(); //update all the word tables
            
            loader.UpdateSiteStats(); //update some key values in qa_stats

        }
    }
}
