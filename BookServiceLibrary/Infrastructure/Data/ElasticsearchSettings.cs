using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Infrastructure.Data
{
    public class ElasticsearchSettings
    {
        public string Uri { get; set; } = "http://127.0.0.1:9200/"; 
        public string IndexName { get; set; } = "books-index";  
    }
}
