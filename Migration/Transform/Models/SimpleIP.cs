using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportSiteETL.Migration.Transform.Models
{
    //simple ip class for comparisons
    public class SimpleIP
    {
        List<int> parts; //4 parts

        public SimpleIP() //default constructor
        {
            parts = new List<int> { 0, 0, 0, 0 };
        }

        public SimpleIP(int d1, int d2, int d3, int d4)
        {
            parts = new List<int> { d1, d2, d3, d4 };
        }

        public SimpleIP(string ip)
        {
            List<string> partsStr = ip.Split(".").ToList();
            parts = new List<int>();

            foreach (string p in partsStr)
                parts.Add(int.Parse(p));
        }

        //overload some operators to make the class useful
        public static bool operator <(SimpleIP left, SimpleIP right)
        {
            for (int i = 0; i < 4; i++)
            {
                if (left.parts[i] < right.parts[i])
                    return true;
                else if (left.parts[i] > right.parts[i])
                    return false;
                //otherwise segment is the same
            }
            return false; //must be equal
        }
        public static bool operator >(SimpleIP left, SimpleIP right)
        {
            for (int i = 0; i < 4; i++)
            {
                if (left.parts[i] > right.parts[i])
                    return true;
                else if (left.parts[i] < right.parts[i])
                    return false;
                //otherwise segment is the same
            }
            return false; //must be equal
        }

        public static bool operator ==(SimpleIP left, SimpleIP right)
        {
            for (int i = 0; i < 4; i++)
                if (left.parts[i] != right.parts[i]) //not equal
                    return false;
            return true; //all parts match
        }
        public static bool operator !=(SimpleIP left, SimpleIP right)
        {
            for (int i = 0; i < 4; i++)
                if (left.parts[i] != right.parts[i]) //not equal
                    return true;
            return false; //all parts match
        }

        public static bool operator <=(SimpleIP left, SimpleIP right)
        {
            return (left == right || left < right);
        }
        public static bool operator >=(SimpleIP left, SimpleIP right)
        {
            return (left == right || left > right);
        }
    }
}
