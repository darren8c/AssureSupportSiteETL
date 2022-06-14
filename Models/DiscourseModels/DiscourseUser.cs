using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Models.DiscourseModels
{
    public class DiscourseUser
    {
        #region Properties
        public int Id { get; set; }

        public string Username { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        
        public string Name { get; set; }
        
        public int SeenNotificationID { get; set; }
        
        public DateTime LastPostedAt { get; set; }
        
        public string PasswordHash { get; set; }
        
        public string Salt { get; set; }
        
        public bool Active { get; set; }
        
        public string UsernameLower { get; set; }
        
        public DateTime LastSeenAt { get; set; }
        
        public bool Admin { get; set; }
        
        public DateTime LastEmailedAt { get; set; }
        
        public int TrustLevel { get; set; }
        
        public bool Approved { get; set; }
        
        public int ApprovedById { get; set; }
        
        public DateTime ApprovedAt { get; set; }
        
        public DateTime PreviousVisitAt { get; set; }
        
        public DateTime SuspendedAt { get; set; }
        
        public DateTime SuspendedTill { get; set; }
        
        public DateTime DateOfBirth { get; set; }

        public int Views { get; set; }
        
        public int FlagLevel { get; set; }
        
        public string IpAddress { get; set; }
        
        public bool Moderator { get; set; }
        
        public string Title { get; set; }
        
        public int UploadedAvatarID { get; set; }
        
        public string Locale { get; set; }
        
        public int PrimaryGroupId { get; set; }
        
        public string RegistrationIpAddress { get; set; }
        
        public bool Staged { get; set; }
        
        public DateTime FirstSeenAt { get; set; }
        
        public DateTime SilencedTill { get; set; }
        
        public int GroupLockedTrustLevel { get; set; }
        
        public int ManualLockedTrustLevel { get; set; }
        
        public string SecureIdentifier { get; set; }
        
        public int FlairGroupId { get; set; }
        
        #endregion

        public DiscourseUser(int id, string username, DateTime created_at, DateTime updated_at, string name, int seen_notification_id, DateTime last_posted_at, string password_hash, string salt, bool active, string username_lower, DateTime last_seen_at, bool admin, DateTime last_emailed_at, int trust_level, bool approved, int approved_by_id, DateTime approved_at, DateTime previous_visit_at, DateTime suspended_at, DateTime suspended_till, DateTime date_of_birth, int views, int flag_level, string ip_address, bool moderator, string title, int uploaded_avatar_id, string locale, int primary_group_id, string registration_ip_address, bool staged, DateTime first_seen_at, DateTime silenced_till, int group_locked_trust_level, int manual_locked_trust_level, string secure_identifier, int flair_group_id) {
            Id = id;
            Username = username;
            CreatedAt = created_at;
            UpdatedAt = updated_at;
            Name = name;
            SeenNotificationID = seen_notification_id;
            LastPostedAt = last_posted_at;
            PasswordHash = password_hash;
            Salt = salt;
            Active = active;
            UsernameLower = username_lower;
            LastSeenAt = last_seen_at;
            Admin = admin;
            LastEmailedAt = last_emailed_at;
            TrustLevel = trust_level;
            Approved = approved;
            ApprovedById = approved_by_id;
            ApprovedAt = approved_at;
            PreviousVisitAt = previous_visit_at;
            SuspendedAt = suspended_at;
            SuspendedTill = suspended_till;
            DateOfBirth = date_of_birth;
            Views = views;
            FlagLevel = flag_level;
            IpAddress = ip_address;
            Moderator = moderator;
            Title = title;
            UploadedAvatarID = uploaded_avatar_id;
            Locale = locale;
            PrimaryGroupId = primary_group_id;
            RegistrationIpAddress = registration_ip_address;
            Staged = staged;
            FirstSeenAt = first_seen_at;
            SilencedTill = silenced_till;
            GroupLockedTrustLevel = group_locked_trust_level;
            ManualLockedTrustLevel = manual_locked_trust_level;
            SecureIdentifier = secure_identifier;
            FlairGroupId = flair_group_id;
        }

         // Returns the `NAME: VAUE` of every field, separated by newlines
        public override string ToString()
        {
            // Modified from https://stackoverflow.com/questions/4023462/how-do-i-automatically-display-all-properties-of-a-class-and-their-values-in-a-s
            return GetType().GetProperties()
                .Select(info => (info.Name, Value: info.GetValue(this, null) ?? "NULL"))
                .Aggregate(
                    new StringBuilder(),
                    (sb, pair) => sb.AppendLine($"{pair.Name}: {pair.Value}"),
                    sb => sb.ToString());
        }

        // Returns the comma separated values of every field of this object
        public string ToCSV()
        {
            // Modified from https://stackoverflow.com/questions/4023462/how-do-i-automatically-display-all-properties-of-a-class-and-their-values-in-a-s
            return GetType().GetProperties()
                .Select(info => (info.Name, Value: info.GetValue(this, null) ?? "NULL"))
                .Aggregate(
                    new StringBuilder(),
                    (sb, pair) => sb.Append($"{pair.Value}, "),
                    sb => sb.ToString()).TrimEnd().TrimEnd(',');
        }
    }
}
