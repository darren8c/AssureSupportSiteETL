using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Migration.Transform.Models
{
    public class ImageBlob
    {
        public UInt64 blobid; //random blobid, 64 bit int unsigned
        public string format; //should be png or jpeg
        public string? filename; //name of file, doesn't need to be set
        public int? userid; //user who uploaded
        public int? cookieid; //cookie id, leave as null
        //public string createip; //ip of uploader, this doesn't need to be set
        public DateTime created; //time of upload

        public Byte[] content; //binary data of the image

        public ImageBlob() //just set default values for the constructor
        {
            blobid = 0;
            format = "";
            filename = null;
            userid = 0;
            cookieid = null;

            //content won't be set yet
        }
    }
}
