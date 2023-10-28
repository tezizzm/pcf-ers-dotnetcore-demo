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
    public DemoFragmentHelper(ICompositeViewEngine viewEngine, IViewBufferScope viewBufferScope) : base(viewEngine, viewBufferScope)
    {
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        string originalName = Name;
        string platform;
        if (Platform.IsCloudFoundry)
        {
            platform = "Tas";
        }
        else
        {
            platform = "Asa";
        }
        try
        {
            Name = $"{platform}/{Name}";
            await base.ProcessAsync(context, output);
        }
        finally
        {
            Name = originalName;
        }

    }


}