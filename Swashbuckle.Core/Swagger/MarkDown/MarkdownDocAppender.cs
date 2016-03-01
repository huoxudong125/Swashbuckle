using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Swashbuckle.Swagger
{
    public class MarkdownDocAppender
    {
        private const string INDENT_STRING = "    ";

        public static void CreateMarkDownFrom(SwaggerDocument swDoc)
        {
            var docPath = GetDocPath();
            CreateCatalog(swDoc, docPath);
            CreateMethods(swDoc, docPath);
            using (var writer = File.AppendText(Path.ChangeExtension(docPath, "source.md")))
            {
                writer.Write(JsonConvert.SerializeObject(swDoc,
                    new JsonSerializerSettings { Formatting = Formatting.Indented }));
            }

            var docFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Doc");
            var lastDocFile = Path.Combine(docFolder, @"api_document.md");
            if (!Directory.Exists(docFolder))
            {
                Directory.CreateDirectory(docFolder);
            }
            if (File.Exists(lastDocFile))
            {
                File.Delete(lastDocFile);
            }

            File.Move(docPath, Path.Combine(docFolder, lastDocFile));
        }

        private static void CreateMethods(SwaggerDocument swDoc
            , string docPath)
        {
            if (swDoc != null)

            {
                using (var writer = File.AppendText(docPath))
                {
                    foreach (var pathItem in swDoc.paths)
                    {
                        var operation = CreateOperation(pathItem, docPath, writer);

                        if (operation != null)
                        {
                            GetParas(operation, writer);

                            CreateResponse(swDoc, operation, writer);
                        }

                        writer.WriteLine("\n\n");
                        writer.WriteLine(new string('-', 50));
                    }
                }
            }
        }

        private static void CreateResponse(SwaggerDocument swDoc, Operation operation, StreamWriter writer)
        {
            var lineFormat = "{0}      \n";
            if (operation.responses.Any())
            {
                writer.WriteLine(lineFormat, "### **返回结果**");

                foreach (var response in operation.responses)
                {
                    var schema = response.Value.schema;
                    if (schema != null)
                    {
                        writer.WriteLine(lineFormat, "**Http 状态码：** `" + response.Key + "`");
                        writer.WriteLine(lineFormat,
                            "**Http 结果描述：** `" + response.Value.description + "`");

                        var responseFormat = @"
```javascript

{0}

```
";
                        var schemaResult = GetSchemes(swDoc, schema);
//                        writer.WriteLine(responseFormat, FormatJson(schemaResult));
                        writer.WriteLine(responseFormat, JsonHelper.FormatJson(schemaResult));
                    }
                }
            }
        }

        private static string GetSchemes(SwaggerDocument swDoc, Schema schema)
        {
            if (schema == null)
            {
                return string.Empty;
            }

            var responseBuilder = new StringBuilder();
            var lineFormat = "{0}      \n";

            if (schema.@ref != null)
            {
                var objectSchema =
                    swDoc.definitions[schema.@ref.Replace(@"#/definitions/", string.Empty)];

                responseBuilder.Append("{");
                foreach (var item in objectSchema.properties)
                {
                    if ( item.Value.type == "array" && item.Value.items.@ref!=null)
                    {
                       
                        if (item.Value.items.@ref == schema.@ref)
                        {
                        
                            responseBuilder.AppendFormat("\"{0}\":[{1}:{2}:{3}]", item.Key, item.Value.type,
                                item.Value.items.@ref.Replace(@"#/definitions/", string.Empty), item.Value.description);
                            continue;
                        }
                        else if(item.Value!=null)
                        {
                            var arrayLineFormat = "[{0}]";
                            var result = GetSchemes(swDoc, item.Value);
                            responseBuilder.AppendFormat(arrayLineFormat, result);
                        }
                    }
                    else if (item.Value.@ref != null)
                    {
                        var subRef = swDoc.definitions[item.Value.@ref.Replace(@"#/definitions/", string.Empty)];
                        responseBuilder.AppendFormat(lineFormat, GetSchemes(swDoc, subRef));
                    }
                   

                    responseBuilder.Append(
                        "\"" + item.Key + "\" : \""
                        + item.Value.type
                        + (item.Value.format == null ? string.Empty : string.Format("({0})", item.Value.format))
                        + ":" + item.Value.description
                        + "\",");
                }
                responseBuilder.Append("}");
            }

            if (schema.type == "array")
            {
                responseBuilder.Append("[");
                if (schema.items.@ref != null)
                {
                    responseBuilder.Append(schema.type+":"+ schema.items.@ref.Replace(@"#/definitions/", string.Empty));
                }
                else
                {
                    responseBuilder.Append(schema.items.type);
                }
                var result = GetSchemes(swDoc, schema.items);

                responseBuilder.AppendFormat("{0}]", result);
            }

            return responseBuilder.ToString();
        }

        private static void GetParas(Operation operation, StreamWriter writer)
        {
            var lineFormat = "{0}      \n";
            writer.WriteLine(lineFormat, "### **请求参数** ");
            if (operation.parameters != null)
            {
                var paraLineFormat = " {0} | {1}| {2} | {3} | {4}  ";

                writer.WriteLine(paraLineFormat, "字段", "必选", "类型", "输入", "说明");
                writer.WriteLine(paraLineFormat, "---", "---", "---", "---", "---");
                foreach (var para in operation.parameters)
                {
                    writer.WriteLine(paraLineFormat
                        , para.name
                        , para.required
                        , para.type
                        , para.@in
                        , para.description);
                }
            }
            else
            {
                writer.WriteLine(lineFormat, "无");
            }
        }

        private static Operation CreateOperation(KeyValuePair<string, PathItem> pathItem, string docPath,
            StreamWriter writer)
        {
            var lineFormat = "{0}      \n";
            var urlLineFormat = "## [{0}]({1})        ";

            Operation operation = null;
            if (pathItem.Value.get != null)
            {
                operation = pathItem.Value.get;
                writer.WriteLine(urlLineFormat,
                    string.IsNullOrEmpty(operation.summary) ? pathItem.Key : operation.summary, pathItem.Key);
                writer.WriteLine(lineFormat, "### **URL:** `" + pathItem.Key + "`");
                writer.WriteLine(lineFormat, ">" + operation.description);

                writer.WriteLine(lineFormat, "### **HTTP请求方式** `Get`");
            }
            else if (pathItem.Value.post != null)
            {
                operation = pathItem.Value.post;
                if (operation != null)
                {
                    writer.WriteLine(urlLineFormat,
                        string.IsNullOrEmpty(operation.summary) ? pathItem.Key : operation.summary, pathItem.Key);
                    writer.WriteLine(lineFormat, "### **URL:** `" + pathItem.Key + "`");
                    writer.WriteLine(lineFormat, ">" + operation.description);

                    writer.WriteLine(lineFormat, "### **HTTP请求方式** `Post`");
                }
            }
            else if (pathItem.Value.put != null)
            {
                operation = pathItem.Value.put;
                if (operation != null)
                {
                    writer.WriteLine(urlLineFormat,
                        string.IsNullOrEmpty(operation.summary) ? pathItem.Key : operation.summary, pathItem.Key);
                    writer.WriteLine(lineFormat, "### **URL:** `" + pathItem.Key + "`");
                    writer.WriteLine(lineFormat, ">" + operation.description);
                    writer.WriteLine(lineFormat, "### **HTTP请求方式** `Put`");
                }
            }
            else if (pathItem.Value.delete != null)
            {
                operation = pathItem.Value.delete;
                if (operation != null)
                {
                    writer.WriteLine(urlLineFormat,
                        string.IsNullOrEmpty(operation.summary) ? pathItem.Key : operation.summary, pathItem.Key);
                    writer.WriteLine(lineFormat, "### **URL:** `" + pathItem.Key + "`");
                    writer.WriteLine(lineFormat, ">" + operation.description);
                    writer.WriteLine(lineFormat, "### **HTTP请求方式** `Delete`");
                }
            }
            else if (pathItem.Value.head != null)
            {
                operation = pathItem.Value.head;
                if (operation != null)
                {
                    writer.WriteLine(urlLineFormat,
                        string.IsNullOrEmpty(operation.summary) ? pathItem.Key : operation.summary, pathItem.Key);
                    writer.WriteLine(lineFormat, "### **URL:** `" + pathItem.Key + "`");
                    writer.WriteLine(lineFormat, ">" + operation.description);
                    writer.WriteLine(lineFormat, "### **HTTP请求方式** `Head`");
                }
            }
            else if (pathItem.Value.options != null)
            {
                operation = pathItem.Value.options;
                if (operation != null)
                {
                    writer.WriteLine(urlLineFormat,
                        string.IsNullOrEmpty(operation.summary) ? pathItem.Key : operation.summary, pathItem.Key);
                    writer.WriteLine(lineFormat, "### **URL:** `" + pathItem.Key + "`");
                    writer.WriteLine(lineFormat, ">" + operation.description);

                    writer.WriteLine(lineFormat, "### **HTTP请求方式** `Options`");
                }
            }

            return operation;
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
                    writer.WriteLine(lineFormat, swDoc.info.version);
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

        //private static string FormatJson(string json)
        //{
        //    var indentation = 0;
        //    var quoteCount = 0;
        //    var result =
        //        from ch in json
        //        let quotes = ch == '"' ? quoteCount++ : quoteCount
        //        let lineBreak =
        //            ch == ',' && quotes % 2 == 0
        //                ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(INDENT_STRING, indentation))
        //                : null
        //        let openChar =
        //            ch == '{' || ch == '['
        //                ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(INDENT_STRING, ++indentation))
        //                : ch.ToString()
        //        let closeChar =
        //            ch == '}' || ch == ']'
        //                ? Environment.NewLine + string.Concat(Enumerable.Repeat(INDENT_STRING, --indentation)) + ch
        //                : ch.ToString()
        //        select lineBreak == null
        //            ? openChar.Length > 1
        //                ? openChar
        //                : closeChar
        //            : lineBreak;

        //    return string.Concat(result);
        //}
    }
}