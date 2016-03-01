using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Swashbuckle.Dummy.Controllers
{
 
    public class RecursiveTypeController:ApiController
    {
        [HttpGet]
        public List<MenuItem> GetMenuItems()
        {
            throw new NotImplementedException();
        }
    }
    public class MenuItem
    {

        public String description;
        public String title;

        public List<MenuItem> sub;

        public MenuItem(String title, String description)
        {
            this.title = title;
            this.description = description;
            this.sub = new List<MenuItem>();
        }

    }
}
