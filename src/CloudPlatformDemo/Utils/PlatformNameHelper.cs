using CloudPlatformDemo.Workaround;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Steeltoe.Common;

namespace CloudPlatformDemo.Utils;

/// <summary>
/// Renders a partial view.
/// </summary>
[HtmlTargetElement("platformname", TagStructure = TagStructure.WithoutEndTag)]
public class PlatformNameHelper : TagHelper
{

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        string platform;
        if (Platform.IsCloudFoundry)
        {
            platform = "Tanzu Application Service";
        }
        else if (Platform2.IsAzureSpringApps)
        {
            platform = "Azure Spring Apps Enterprise";
        }
        else if (Platform2.IsTanzuApplicationPlatform)
        {
            platform = "Tanzu Platform";
        }
        else
        {
            platform = "Containers";
        }

        output.Content.SetContent(platform);
        return Task.CompletedTask;
    }


}