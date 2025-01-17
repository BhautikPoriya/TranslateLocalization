using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.TranslateLocalization.Infrastructure;

public class TranslateLocalizationPlugin : BasePlugin, IMiscPlugin
{
    #region Fields 

    private readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public TranslateLocalizationPlugin(IWebHelper webHelper)
    {
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _webHelper.GetStoreLocation() + "Admin/TranslateLocalization/Translate";
    }

    #endregion
}
