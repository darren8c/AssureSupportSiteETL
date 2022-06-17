﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupportSiteETL.Databases;
using MySql.Data.MySqlClient;
using SupportSiteETL.Migration.Transform;

namespace SupportSiteETL.Migration.Load
{
    //handles the specific sql queries for the q2a site (includes reading and writing)
    //discourse requires no loading, so no discourse calls
    public class Loader
    {
        public Q2AConnection q2a;

        public Loader()
        {
            q2a = new Q2AConnection();
        }
        
        //updates the cache_userpointscount stat which tracks the number of users on the site
        public void UpdateUserCount()
        {
            //update the user count field in the options table
            MySqlConnection conn = q2a.retrieveConnection();
            int totalUsers = 1; //a query will determine the count

            string findUserCountCommand = "SELECT COUNT(*) FROM qa_users";
            string userCountUpdateCommand = "UPDATE qa_options SET content = @content WHERE title='cache_userpointscount'";
            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(findUserCountCommand, conn)) //count number of users
                {
                    totalUsers = (int)(Int64)cmd.ExecuteScalar(); //executes query and returns entry in first row and column
                }
                using (MySqlCommand cmd = new MySqlCommand(userCountUpdateCommand, conn)) //write to qa_users
                {
                    cmd.Parameters.AddWithValue("@content", totalUsers);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding user count data: " + ex.Message);
            }
            conn.Close();
        }

        //add user data to all the different tables given user data
        public void addUser(Q2AUserData user)
        {
            MySqlConnection conn = q2a.retrieveConnection();

            // Queries to insert users
            string sql4Users = "INSERT INTO qa_users (userid, created, createip, loggedin, loginip, email, handle, level, flags, wallposts) VALUES (@userid, @created, @createip, @loggedin, @loginip, @email, @handle, @level, @flags, @wallposts)";
            string sql4Points = "Insert INTO qa_userpoints (userid, points, qposts, qupvotes, qvoteds, upvoteds) VALUES (@userid, @points, @qposts, @qupvotes, @qvoteds, @upvoteds)";
            //the profile writing is actually 4 inserts
            string sql4Profiles = "Insert INTO qa_userprofile (userid, title, content) VALUES (@userid, @title, @content)";

            conn.Open();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(sql4Users, conn)) //to qa_users
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@created", user.created_at);
                    cmd.Parameters.AddWithValue("@createip", "");
                    cmd.Parameters.AddWithValue("@loggedin", user.loggedin);
                    cmd.Parameters.AddWithValue("@loginip", "");
                    cmd.Parameters.AddWithValue("@email", user.email);
                    cmd.Parameters.AddWithValue("@handle", user.handle);
                    cmd.Parameters.AddWithValue("@level", user.level);
                    cmd.Parameters.AddWithValue("@flags", user.flags);
                    cmd.Parameters.AddWithValue("@wallposts", user.wallposts);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Points, conn)) //to qa_points
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@qposts", user.qposts);
                    cmd.Parameters.AddWithValue("@qupvotes", user.qupvotes);
                    cmd.Parameters.AddWithValue("@qvoteds", user.qvoteds);
                    cmd.Parameters.AddWithValue("@upvoteds", user.upvoteds);
                    cmd.Parameters.AddWithValue("@points", user.points);
                    cmd.ExecuteNonQuery();
                }
                //each of the profile writes
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //about
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "about");
                    cmd.Parameters.AddWithValue("@content", user.about);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //location
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "location");
                    cmd.Parameters.AddWithValue("@content", user.location);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //name
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "name");
                    cmd.Parameters.AddWithValue("@content", user.name);
                    cmd.ExecuteNonQuery();
                }
                using (MySqlCommand cmd = new MySqlCommand(sql4Profiles, conn)) //website
                {
                    cmd.Parameters.AddWithValue("@userid", user.userId);
                    cmd.Parameters.AddWithValue("@title", "website");
                    cmd.Parameters.AddWithValue("@content", user.website);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine(string.Format("Inserted user with ID: {0} - {1}", user.userId, user.handle));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding new user: " + ex.Message);
            }
            conn.Close();
        }
    }

}
