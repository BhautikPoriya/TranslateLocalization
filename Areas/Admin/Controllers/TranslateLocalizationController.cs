using Microsoft.AspNetCore.Http;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Nop.Plugin.Misc.TranslateLocalization.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class TranslateLocalizationController : BasePluginController
{
    #region Fields

    protected readonly IPermissionService _permissionService;
    private readonly IHttpClientFactory _httpClientFactory;

    #endregion

    #region Ctor

    public TranslateLocalizationController(IPermissionService permissionService,
        IHttpClientFactory httpClientFactory)
    {
        _permissionService = permissionService;
        _httpClientFactory = httpClientFactory;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Translate()
    {
        return View($"~/Plugins/Misc.TranslateLocalization/Areas/Admin/Views/Translate.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Translate(IFormFile file, string fromLanguage, string toLanguage)
    {
        if (file == null || file.Length == 0)
        {
            ViewBag.ErrorMessage = "Please select a valid XML file.";
            return View($"~/Plugins/Misc.TranslateLocalization/Areas/Admin/Views/Translate.cshtml");
        }

        try
        {
            using (var stream = file.OpenReadStream())
            {
                var document = XDocument.Load(stream);
                var languageElement = document.Root;

                if (languageElement != null && languageElement.Name == "Language")
                {
                    var nameAttribute = languageElement.Attribute("Name");
                    if (nameAttribute != null)
                    {
                        nameAttribute.Value = toLanguage;
                    }
                }

                var elements = document.Descendants("LocaleResource");

                foreach (var element in elements)
                {
                    var valueElement = element.Element("Value");
                    if (valueElement != null)
                    {
                        var originalValue = valueElement.Value;
                        var translatedValue = await TranslateText(originalValue, fromLanguage, toLanguage);
                        valueElement.Value = translatedValue;
                    }
                }

                var newFileName = Path.ChangeExtension(file.FileName, $"from{fromLanguage}to{toLanguage}.translated.xml");
                var newFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", newFileName);

                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));

                document.Save(newFilePath);

                return File(System.IO.File.ReadAllBytes(newFilePath), "application/xml", newFileName);
            }
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
            return View($"~/Plugins/Misc.TranslateLocalization/Areas/Admin/Views/Translate.cshtml");
        }
    }

    private async Task<string> TranslateText(string text, string fromLanguage, string toLanguage)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://google-translate113.p.rapidapi.com/api/v1/translator/html"),
            Headers =
            {
                { "x-rapidapi-key", "364ed3fd03msh00d263e01aee371p1d6b8ajsn369abbb66645" },
                { "x-rapidapi-host", "google-translate113.p.rapidapi.com" },
            },
            Content = new StringContent($"{{\"from\":\"{fromLanguage}\",\"to\":\"{toLanguage}\",\"html\":\"{text}\"}}")
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };

        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(body);
            var translatedText = jsonResponse["trans"]?.ToString();

            if (translatedText == null)
                throw new Exception("Translation API returned null for the translated text.");

            return translatedText;
        }
    }

    #endregion
}
