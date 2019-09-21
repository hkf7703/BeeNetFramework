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
            doc.info = new Info() { title="swagger", version="1.0" };

            var _jsonSerializerSettings = 
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                
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

                        if (pItem.ParameterType.IsPrimitive || pItem.ParameterType == typeof(string))
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
                    //consumes, produces
                    post.consumes = new List<string> { "application/json", "text/json", "application/json-patch+json", "application/*+json" };
                    post.produces = new List<string> { "application/json", "text/plain" , "text/json" };


                    // response
                    var responses = new Dictionary<string, Response>();
                    var responseType = method.ReturnType;

                    responses.Add("200", new Response
                    {
                        description = "OK"
                            ,
                        schema = schemaRegistry.GetOrRegister(typeof(BeeMvcResult))
                    });

                    post.responses = responses;

                    pathItem.post = post;

                    doc.paths.Add(pathKey, pathItem);
                }
                
                doc.tags.Add(tag);
            }

            doc.definitions = schemaRegistry.Definitions;

            if (true)
            {
                doc.securityDefinitions = new Dictionary<string, SecurityScheme>();
                doc.securityDefinitions.Add("Bearer", new SecurityScheme()
                {
                    description = "JWT认证请求头格式: \"Authorization: Bearer {token}\"",
                    @in = "header",
                    name = "Authorization",
                    type = "apiKey"

                });
                doc.security = new List<IDictionary<string, IEnumerable<string>>>();

                doc.security.Add(new Dictionary<string, IEnumerable<string>> {
                    { "Bearer", Enumerable.Empty<string>() }
                });
            }

            var jsonResult = JsonConvert.SerializeObject(doc, Formatting.None, _jsonSerializerSettings);

            return new ContentResult(jsonResult);
        }
    }
}
