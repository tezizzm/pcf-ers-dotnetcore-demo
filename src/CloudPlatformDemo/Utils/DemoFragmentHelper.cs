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
[HtmlTargetElement("demofragment", Attributes = "name", TagStructure = TagStructure.WithoutEndTag)]
public class DemoFragmentHelper : PartialTagHelper
{
    private IViewEngine _viewEngine;
    public DemoFragmentHelper(ICompositeViewEngine viewEngine, IViewBufferScope viewBufferScope) : base(viewEngine, viewBufferScope)
    {
        _viewEngine = viewEngine;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        
        string originalName = Name;
        string platform;
        if (Platform.IsCloudFoundry)
        {
            platform = "Tas";
        }
        else if(Platform2.IsAzureSpringApps)
        {
            platform = "Asa";
        }
        else if(Platform2.IsTanzuApplicationPlatform)
        {
            platform = "Tap";
        }
        else
        {
            platform = "Generic";
        }
        try
        {
            
            Name = $"{platform}/{originalName}";
            var platformSpecificViewExists = _viewEngine.GetView(ViewContext.ExecutingFilePath, $"{Name}.cshtml", false).Success;
            if (!platformSpecificViewExists)
                Name = $"Generic/{originalName}";
            var fallbackViewExists = _viewEngine.GetView(ViewContext.ExecutingFilePath, $"{Name}.cshtml", false).Success;
            if (fallbackViewExists)
            {
                await base.ProcessAsync(context, output);
            }
        }
        finally
        {
            Name = originalName;
        }

    }


}