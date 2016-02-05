using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Swashbuckle.Swagger
{
    public class MarkdownDocumenterAppender
    {
        private const string MainTemplate =
            @"
##{0}

URL | Method
---|---
`~/{0}` | `{1}`

{2}

###Request :

```javascript
{3}
```

{4}

###Response :

```javascript
{5}
```

{6}

***
";

        private const string TableHeaderTemplate =
            @"Name | Type
---|---
";

        private const string TableRowTemplate =
            @"{0} | {1}
";

        //public static string Path = System.IO.Path.Combine(.ApplicationPhysicalPath,
        //    string.Format("api_document_{0:yyyyMd_HHmm}.txt", DateTime.UtcNow));

        public static void CreateMarkDownFrom(SwaggerDocument swDoc)
        {
            var docPath = GetDocPath();
            CreateCatalog(swDoc, docPath);
            CreateMethods(swDoc, docPath);
        }

        private static void CreateMethods(SwaggerDocument swDoc
            , string docPath)
        {
            if (swDoc != null)

            {
                using (var writer = File.AppendText(docPath))
                {
                    var lineFormat = "{0}      \n";
                    var urlLineFormat = "##[{0}]({1})        ";

                    foreach (var pathItem in swDoc.paths)
                    {
                        writer.WriteLine(urlLineFormat, pathItem.Key, pathItem.Key);
                        writer.WriteLine(lineFormat, pathItem.Value.@ref);

                        var operation = pathItem.Value.get;
                        if (operation != null)
                        {
                            writer.WriteLine(lineFormat, "###HTTP请求方式 `Get`");
                            writer.WriteLine(lineFormat, operation.summary);
                        }
                        else
                        {
                            operation = pathItem.Value.post;
                            if (operation != null)
                            {
                                writer.WriteLine(lineFormat, "###HTTP请求方式 `Post`");
                                writer.WriteLine(lineFormat, operation.summary);
                            }
                        }

                        if (operation != null)
                        {
                            if (operation.parameters != null)
                            {
                                writer.WriteLine(lineFormat, "###请求参数 ");
                                var paraLineFormat = " {0} | {1}| {2} | {3}   ";

                                writer.WriteLine(paraLineFormat, "字段", "必选", "类型", "说明");
                                writer.WriteLine(paraLineFormat, "---", "---", "---", "---");
                                foreach (var para in operation.parameters)
                                {
                                    writer.WriteLine(paraLineFormat
                                        , para.name
                                        , para.required
                                        , para.type
                                        , para.description);
                                }
                            }

                            if (operation.responses.Any())
                            {
                                var responseFormat = @"### 返回结果
```javascript

{0}

```
";
                                var responseBuilder = new StringBuilder();
                                foreach (var response in operation.responses)
                                {
                                    var schema = response.Value.schema;
                                    if (schema != null)
                                    {
                                        responseBuilder.AppendFormat(lineFormat, schema.description);
                                        responseBuilder.AppendFormat(lineFormat, schema.@ref);

                                        responseBuilder.AppendFormat(lineFormat, response.Value.schema.type);

                                        var subSchemas = schema.allOf;

                                        //if (subSchemas != null)
                                        //{
                                        if (schema.@ref != null)
                                        {
                                            var objectSchema = swDoc.definitions[schema.@ref.Replace(@"#/definitions/",String.Empty)];
                                            foreach (var item in objectSchema.properties)
                                            {
                                                responseBuilder.AppendFormat(lineFormat, item.Key);
                                            }
                                        }
                                        //}
                                    }
                                }
                                writer.WriteLine(responseFormat, responseBuilder);
                            }
                        }

                        writer.WriteLine("\n\n");
                        writer.WriteLine(new string('-', 50));
                    }
                }
            }
        }

        private static void CreateCatalog(SwaggerDocument swDoc
            , string docPath)
        {
            if (swDoc != null)

            {
                using (var writer = File.AppendText(docPath))
                {
                    var lineFormat = "{0}      ";

                    writer.WriteLine("# 文档基础信息");
                    writer.WriteLine(lineFormat, swDoc.info.title);
                    writer.WriteLine(lineFormat, swDoc.info);
                    writer.WriteLine(lineFormat, swDoc.info.description);
                    writer.WriteLine(new string('-', 50));

                    writer.WriteLine("# 目录");
                    var urlLineFormat = "[{0}]({1})        ";
                    foreach (var pathItem in swDoc.paths)
                    {
                        writer.WriteLine(urlLineFormat, pathItem.Key, pathItem.Key);
                    }
                    writer.WriteLine("\n\n");
                    writer.WriteLine(new string('-', 50));
                }
            }
        }

        private static string GetDocPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            }
            var path = Path.Combine(baseDirectory, string.Format("api_document_{0:yyyyMMdd_HHmm}.md", DateTime.Now));

            return path;
        }
    }
}