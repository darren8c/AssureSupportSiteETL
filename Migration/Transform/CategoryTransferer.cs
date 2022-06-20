using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Migration.Extract;
using SupportSiteETL.Migration.Transform;
using SupportSiteETL.Migration.Transform.Models;
using SupportSiteETL.Migration.Load;

namespace SupportSiteETL.Migration.Transform
{
    using Category = Dictionary<string, string>;
    public class CategoryTransferer
    {

        private List<Q2ACategory> q2aCatsOld; //categories already on q2a
        private List<Q2ACategory> q2aCatsNew; //new categories to be written to q2a

        private Extractor extractor;
        private Loader loader;

        public Dictionary<int, int> catIdMap; //maps discourse category id to q2a category id
        private Dictionary<int, Q2ACategory> oldCategoryMap; //maps old discourse categories id's to new one's on q2a

        public CategoryTransferer()
        {
            q2aCatsOld = new List<Q2ACategory>();
            q2aCatsNew = new List<Q2ACategory>();
            extractor = new Extractor();
            loader = new Loader();

            catIdMap = new Dictionary<int, int>();
            oldCategoryMap = new Dictionary<int, Q2ACategory>();

            populateCategoryMap(); //get map from old discourse id to new category
        }

        //read the necessary data from the databases, generates list of categories which need to be added
        public void Extract()
        {
            List<Category> q2aCurrCats = extractor.GetQ2ACategories(); //the categories that exists on q2a currently
            int currPosition = 0; //current category position
            int currId = 0; //current categoryid
            foreach(Category c in q2aCurrCats) //find the currPosition and currId, needed for adding any new categories
            {
                if (int.Parse(c["categoryid"]) > currId) //new max
                    currId = int.Parse(c["categoryid"]);
                if (int.Parse(c["position"]) > currPosition) //new max
                    currPosition = int.Parse(c["position"]);
            }
            currPosition++;
            currId++; //these should be iterated to go from last id to next available id.

            foreach(Q2ACategory newCat in oldCategoryMap.Values) //go through each category in our table, add them if they don't exist in q2a
            {
                if (categoryExists(newCat)) //we're only concerned with distinct values
                    continue;

                bool alreadyExists = false; //check if the category already exists in the q2a site
                int existingId = -1; //if the id exists in q2a
                foreach(Category c in q2aCurrCats)
                {
                    if (c["title"] == newCat.title)
                    {
                        alreadyExists = true;
                        existingId = int.Parse(c["categoryid"]);
                        break;
                    }
                }

                //if the categorry is new, add it to the list, with the necessary category info
                if (!alreadyExists)
                {
                    //fill with necessary info
                    newCat.parentid = null;
                    newCat.position = currPosition;
                    newCat.id = currId;
                    newCat.GenBackpath(); //set the tags and backpath fields
                    //content and title are already set
                    //qcount will have to be set later

                    //we used the id and position, iterate for the next new category
                    currId++;
                    currPosition++;

                    q2aCatsNew.Add(newCat);
                }
                else
                {
                    newCat.id = existingId; //if the category already exists we still have to know the mapped id
                    q2aCatsOld.Add(newCat);
                }
            }
            populateIdMap(); //setup up the translator map from discourse id to q2a id
        }
        
        //write the new categories to the database
        public void Load()
        {
            foreach (Q2ACategory category in q2aCatsNew) //add each new category
                loader.addCategory(category);
        }

        //update the qcount fields for each of the entries in q2a, assumes post data is already loaded on q2a
        public void updateCategoryCounts()
        {
            List<Category> q2aCurrCats = extractor.GetQ2ACategories(); //the categories that exists on q2a currently
            foreach(Category c in q2aCurrCats)
                loader.UpdateCategoryCount(int.Parse(c["categoryid"])); //update the count for this id #
        }

        //check if a category with the same name already exists in old or new list, helper function to Extract();
        private bool categoryExists(Q2ACategory c)
        {
            foreach (Q2ACategory category in q2aCatsOld)
                if (category.title == c.title) //match
                    return true;
            foreach (Q2ACategory category in q2aCatsNew)
                if (category.title == c.title) //match
                    return true;
            return false; //no match
        }

        //populate the old category map, maps discourse id's to q2a name
        private void populateCategoryMap()
        {
            //fill in the lookup table from old discourse category id to their new category name and description under Q2A
            var lines = File.ReadLines("Resources/categoryMappings.txt");
            foreach (string line in lines)
            {
                string[] data = line.Split(',');
                if (data.Length != 3) //there should always be 3 fields (discourse_id, categoryName, category description)
                {
                    Console.WriteLine("Error, categoryMappings.txt is not in the correct format!");
                    return;
                }
                int id = int.Parse(data[0]);
                string name = data[1];
                string descr = data[2];

                Q2ACategory tempCat = new Q2ACategory();
                tempCat.title = name;
                tempCat.content = descr;

                oldCategoryMap.Add(id, tempCat);
            }
        }

        //given the curr and new category id's are known, fill the dictionary
        private void populateIdMap()
        {
            List<Q2ACategory> allQ2ACats = new List<Q2ACategory>();
            allQ2ACats.AddRange(q2aCatsOld);
            allQ2ACats.AddRange(q2aCatsNew);
            foreach (int discId in oldCategoryMap.Keys)
            {
                int q2aId = -1;
                string catTitle = oldCategoryMap[discId].title;
                foreach (Q2ACategory category in allQ2ACats)
                {
                    if (category.title == catTitle) //match
                    {
                        q2aId = category.id;
                        break;
                    }
                }
                catIdMap.Add(discId, q2aId);
            }
        }
    }
}
