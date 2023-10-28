using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Steeltoe.Common;

namespace CloudPlatformDemo.Utils;

public static class HtmlHelperPartialExtensions
{
    public static Task<IHtmlContent> DemoFragment(this IHtmlHelper htmlHelper, string partialViewName)
    {
        string platform;
        if (Platform.IsCloudFoundry)
        {
            platform = "Tas";
        }
        else
        {
            platform = "Asa";
        }

        return htmlHelper.PartialAsync($"{platform}/{partialViewName}");
    }
}