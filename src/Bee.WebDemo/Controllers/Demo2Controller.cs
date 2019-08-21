using Bee.Util;
using Bee.Web;
using Bee.Web.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bee.WebDemo.Controllers
{

    public class Demo2Controller : APIControllerBase
    {

        public int Add(int i, int j)
        {
            return i + j;
        }

        public void Create2(BeeDataAdapter dataAdapter)
        {

            ThrowExceptionUtil.ArgumentConditionTrue(dataAdapter.TryGetValue<string>("name", string.Empty) == "admin", string.Empty, "not null");
        }

        public void Create(Person person)
        {
            ThrowExceptionUtil.ArgumentConditionTrue(person.Name == "admin", string.Empty, "not null");
        }

        public JsonResult Test()
        {
            return null;
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Sex { get; set; }
        public DateTime BirthDate { get; set; }
    }
}