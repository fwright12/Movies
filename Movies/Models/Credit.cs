using System;
using System.Collections.Generic;
using System.Text;

namespace Movies.Models
{
    public class Credit
    {
        public string Role { get; set; }
        public Person Person { get; set; }
        public string Department { get; set; }
    }

    public class TVCredit
    {
        public int Episodes { get; set; }
    }
}
