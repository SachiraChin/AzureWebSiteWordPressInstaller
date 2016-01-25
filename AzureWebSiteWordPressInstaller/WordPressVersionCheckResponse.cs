using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureWebSiteWordPressInstaller
{
    public class Packages
    {
        public string full { get; set; }
        public string no_content { get; set; }
        public string new_bundled { get; set; }
        public bool partial { get; set; }
        public bool rollback { get; set; }
    }

    public class Offer
    {
        public string response { get; set; }
        public string download { get; set; }
        public string locale { get; set; }
        public Packages packages { get; set; }
        public string current { get; set; }
        public string version { get; set; }
        public string php_version { get; set; }
        public string mysql_version { get; set; }
        public string new_bundled { get; set; }
        public bool partial_version { get; set; }
        public bool? new_files { get; set; }
    }

    public class WordPressVersionCheckResponse
    {
        public List<Offer> offers { get; set; }
        public List<object> translations { get; set; }
    }
}
