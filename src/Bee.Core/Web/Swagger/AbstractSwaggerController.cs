using Bee.Core;
using Bee.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Web.Swagger
{
    public abstract class AbstractSwaggerController : APIControllerBase
    {
        public virtual ActionResult Index()
        {
            SwaggerDocument doc = new SwaggerDocument();
            Uri url = HttpContextUtil.CurrentHttpContext.Request.Url;
            doc.host = "{0}:{1}".FormatWith(url.Host, url.Port);
            doc.basePath = "/";

            var _jsonSerializerSettings = 
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            };
            var schemaRegistry = new SchemaRegistry(
               _jsonSerializerSettings, true, true, true, true);

            doc.paths = new Dictionary<string, PathItem>();
            doc.tags = new List<Tag>();

            foreach(var item in ControllerManager.Instance.Keys)
            {
                Tag tag = new Tag() { name = item };

                ControllerInfo controllerInfo = ControllerManager.Instance.GetControllerInfo(item);
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(controllerInfo.Type);

                List<MethodSchema> methodList = entityProxy.GetMethodList();
                foreach (var method in methodList)
                {
                    string pathKey = "/{0}/{1}".FormatWith(item, method.Name);

                    PathItem pathItem = new PathItem();
                    Operation post = new Operation();
                    post.tags = new List<string>() { item };

                    post.parameters = new List<Parameter>();
                    foreach(var pItem in method.ParameterInfos)
                    {
                        Parameter parameter = new Parameter();
                        parameter.name = pItem.Name;

                        if (pItem.ParameterType.IsPrimitive)
                        {
                            parameter.@in = "query";
                        }
                        else
                        {
                            parameter.@in = "body";
                        }

                        var schema = schemaRegistry.GetOrRegister(pItem.ParameterType);
                        if (parameter.@in == "body")
                            parameter.schema = schema;
                        else
                            parameter.PopulateFrom(schema);

                        post.parameters.Add(parameter);
                    }


                    // response
                    var responses = new Dictionary<string, Response>();
                    var responseType = method.ReturnType;
                    if (responseType == null || responseType == typeof(void))
                        responses.Add("204", new Response { description = "No Content" });
                    else
                        responses.Add("200", new Response { description = "OK"
                            , schema = schemaRegistry.GetOrRegister(responseType) });

                    post.responses = responses;

                    pathItem.post = post;

                    doc.paths.Add(pathKey, pathItem);
                }
                
                doc.tags.Add(tag);
            }

            doc.definitions = schemaRegistry.Definitions;

            var jsonResult = JsonConvert.SerializeObject(doc, Formatting.None, _jsonSerializerSettings);

            return new ContentResult(jsonResult);
        }
    }
}
