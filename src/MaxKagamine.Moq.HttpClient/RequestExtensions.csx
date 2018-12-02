using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

string path = Path.Combine(Path.GetDirectoryName(ScriptFilePath), "RequestMatcher.cs");
string file = File.ReadAllText(path);

var matches = Regex.Matches(file, @"(?<xml>(?: *\/{3}\s*(?:<param name=""(?<paramname>[^""]+))?.*\r?\n)+)\s*public static HttpRequestMessage Is\((?<params>.*?)\)");

private class GenerateOptions
{
    /// <summary>
    /// The name of the #region surrounding the methods.
    /// </summary>
    public string RegionName { get; set; }

    /// <summary>
    /// The beginning of the summary, replacing the bracketed portion of "[A method] matching..." in the RequestMatcher methods.
    /// </summary>
    public string SummaryPrefix { get; set; }

    /// <summary>
    /// The summary of the additional "Any" method.
    /// </summary>
    public string AnyMethodSummary { get; set; }

    /// <summary>
    /// Additional xml docs inserted before the original &lt;param /&gt;s. Whitespace before/after each line removed.
    /// </summary>
    public string ExtraXmlBeforeParams { get; set; }

    /// <summary>
    /// Additional xml docs inserted after the original &lt;param /&gt;s. Whitespace before/after each line removed.
    /// </summary>
    public string ExtraXmlAfterParams { get; set; }

    /// <summary>
    /// The method return type.
    /// </summary>
    public string ReturnType { get; set; }

    /// <summary>
    /// The method name.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// The name of the additional "Any" method.
    /// </summary>
    public string AnyMethodName { get; set; }

    /// <summary>
    /// A format string for the method parameters, where {0} is replaced with the parameters of each
    /// RequestMatcher method and removed along with the extra comma for the additional "Any" method.
    /// </summary>
    public string ParamsFormat { get; set; }

    /// <summary>
    /// A format string for the method body, as a single expression after the lamdba arrow, where {0}
    /// is replaced with the RequestMatcher method call, or It.IsAny for the additional "Any" method.
    /// </summary>
    public string BodyFormat { get; set; }
}

private string Generate(GenerateOptions opts)
{
    var str = new StringBuilder();

    // Strip any whitespace in the extra xml
    string FormatExtraXml(string xml) => string.Join($"{Environment.NewLine}        /// ",
        xml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Prepend(""));
    string extraXmlBefore = FormatExtraXml(opts.ExtraXmlBeforeParams ?? "");
    string extraXmlAfter = FormatExtraXml(opts.ExtraXmlAfterParams ?? "");

    // Write a region
    str.AppendLine($"        #region {opts.RegionName}");
    str.AppendLine();

    // Write "any" method
    string anyParams = Regex.Replace(opts.ParamsFormat, @"\s*\{0\}\s*,\s*|\s*,\s*\{0\}\s*", ""); // Remove the placeholder
    str.AppendLine("        /// <summary>");
    str.AppendLine($"        /// {opts.AnyMethodSummary}");
    str.Append("        /// </summary>");
    if (!string.IsNullOrWhiteSpace(extraXmlBefore))
        str.AppendLine(extraXmlBefore);
    if (!string.IsNullOrWhiteSpace(extraXmlAfter))
        str.AppendLine(extraXmlAfter);
    str.AppendLine($"        public static {opts.ReturnType} {opts.AnyMethodName}({anyParams})");
    str.Append("            => ");
    str.AppendFormat(opts.BodyFormat, "It.IsAny<HttpRequestMessage>()");
    str.AppendLine();

    foreach (Match match in matches)
    {
        string xml = match.Groups["xml"].Value;
        string[] paramNames = match.Groups["paramname"].Captures.Cast<Capture>().Select(x => x.Value).ToArray();
        string methodParams = match.Groups["params"].Value;

        // Rewrite summary
        xml = Regex.Replace(xml, @"(?<=<summary>\s*\/{3}\s+)A request", opts.SummaryPrefix);

        // Add extra xml docs
        xml = xml.Insert(xml.IndexOf("</summary>") + "</summary>".Length, extraXmlBefore);
        xml = xml.Insert(xml.LastIndexOf("</param>") + "</param>".Length, extraXmlAfter);

        // Write newline between methods
        str.AppendLine();

        // Write xml doc
        str.Append(xml);

        // Write method signature
        str.AppendLine($"        public static {opts.ReturnType} {opts.MethodName}(");
        str.Append("            ");
        str.AppendFormat(opts.ParamsFormat, methodParams);
        str.AppendLine(")");

        // Write method body
        str.Append("            => ");
        str.AppendFormat(opts.BodyFormat, $"RequestMatcher.Is({string.Join(", ", paramNames)})");
        str.AppendLine();
    }

    // End the region
    str.AppendLine();
    str.Append("        #endregion");

    return str.ToString();
}

Output.WriteLine($@"// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a CSX script.
//     Changes to this file will be lost if the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Language;
using Moq.Language.Flow;

namespace MaxKagamine.Moq.HttpClient
{{
    public static partial class MockHttpMessageHandlerExtensions
    {{
{Generate(new GenerateOptions()
{
    RegionName = "SetupRequest",
    SummaryPrefix = "Specifies a setup for a request",
    AnyMethodSummary = "Specifies a setup matching any request.",
    ExtraXmlBeforeParams = @"<param name=""handler"">The <see cref=""HttpMessageHandler"" /> mock.</param>",
    ReturnType = "ISetup<HttpMessageHandler, Task<HttpResponseMessage>>",
    MethodName = "SetupRequest",
    AnyMethodName = "SetupAnyRequest",
    ParamsFormat = "this Mock<HttpMessageHandler> handler, {0}",
    BodyFormat = "handler.Setup(x => x.SendAsync({0}, It.IsAny<CancellationToken>()));"
})}

{Generate(new GenerateOptions()
{
    RegionName = "SetupRequestSequence",
    SummaryPrefix = "Specifies a setup for a request",
    AnyMethodSummary = "Specifies a setup matching any request.",
    ExtraXmlBeforeParams = @"<param name=""handler"">The <see cref=""HttpMessageHandler"" /> mock.</param>",
    ReturnType = "ISetupSequentialResult<Task<HttpResponseMessage>>",
    MethodName = "SetupRequestSequence",
    AnyMethodName = "SetupAnyRequestSequence",
    ParamsFormat = "this Mock<HttpMessageHandler> handler, {0}",
    BodyFormat = "handler.SetupSequence(x => x.SendAsync({0}, It.IsAny<CancellationToken>()));"
})}

{Generate(new GenerateOptions()
{
    RegionName = "InSequence().SetupRequest",
    SummaryPrefix = "Specifies a setup for a request",
    AnyMethodSummary = "Specifies a setup matching any request.",
    ExtraXmlBeforeParams = @"<param name=""handler"">The <see cref=""HttpMessageHandler"" /> mock.</param>",
    ReturnType = "ISetup<HttpMessageHandler, Task<HttpResponseMessage>>",
    MethodName = "SetupRequest",
    AnyMethodName = "SetupAnyRequest",
    ParamsFormat = "this ISetupConditionResult<HttpMessageHandler> handler, {0}",
    BodyFormat = "handler.Setup(x => x.SendAsync({0}, It.IsAny<CancellationToken>()));"
})}

{Generate(new GenerateOptions()
{
    RegionName = "VerifyRequest",
    SummaryPrefix = "Verifies that a request was sent",
    AnyMethodSummary = "Verifies that any request was sent.",
    ExtraXmlBeforeParams = @"<param name=""handler"">The <see cref=""HttpMessageHandler"" /> mock.</param>",
    ExtraXmlAfterParams = @"<param name=""times"">
                            Number of times that the invocation is expected to have occurred.
                            If omitted, assumed to be <see cref=""Times.AtLeastOnce"" />.
                            </param>
                            <param name=""failMessage"">Message to include in the thrown <see cref=""MockException"" /> if verification fails.</param>",
    ReturnType = "void",
    MethodName = "VerifyRequest",
    AnyMethodName = "VerifyAnyRequest",
    ParamsFormat = "this Mock<HttpMessageHandler> handler, {0}, Times? times = null, string failMessage = null",
    BodyFormat = "handler.Verify(x => x.SendAsync({0}, It.IsAny<CancellationToken>()), times, failMessage);"
})}
    }}
}}");