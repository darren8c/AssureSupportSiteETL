using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Databases;

using MySql.Data.MySqlClient;
using Npgsql;

namespace SupportSiteETL.Migration.Extract
{
    //handles the specific sql queries for the databases related to extracting info
    //includes calls for both q2a and discourse
    public class Extractor
    {
        private DiscourseConnection dc;
        private Q2AConnection q2a;

        public Extractor()
        {
            dc = new DiscourseConnection();
            q2a = new Q2AConnection();
        }

        // Gets all users joined with their user stats
        public List<Dictionary<string, string>> GetDiscourseUsers()
        {
            return dc.ExecuteQuery("SELECT * FROM public.users JOIN public.user_stats ON public.users.id=public.user_stats.user_id ORDER BY id;");
        }


        /// <summary>
        /// Gets all of the users from the Q2A database
        /// </summary>
        /// 
        /// <returns>
        /// A list of users, stored as dictionaries
        /// </returns>
        public List<Dictionary<string, string>> GetQ2AUsers()
        {
            return q2a.ExecuteQuery("SELECT * FROM qa_users ORDER BY userid;");
        }

    }

}
