// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    public class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        ((System.Action)(() => {
#line 1 "x:\dir\subdir\Test\TestComponent.cshtml"
global::System.Object __typeHelper = "*, TestAssembly";

#line default
#line hidden
        }
        ))();
        }
        #pragma warning restore 219
        #pragma warning disable 0414
        private static System.Object __o = null;
        #pragma warning restore 0414
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            __Blazor.Test.TestComponent.TypeInference.CreateMyComponent_0(builder, -1, -1, 
#line 2 "x:\dir\subdir\Test\TestComponent.cshtml"
                     "hi"

#line default
#line hidden
            , -1, (context) => (builder2) => {
#line 3 "x:\dir\subdir\Test\TestComponent.cshtml"
  __o = context.ToLower();

#line default
#line hidden
            }
            );
        }
        #pragma warning restore 1998
    }
}
namespace __Blazor.Test.TestComponent
{
    #line hidden
    internal static class TypeInference
    {
        public static void CreateMyComponent_0<TItem>(global::Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder, int seq, int __seq0, TItem __arg0, int __seq1, global::Microsoft.AspNetCore.Components.RenderFragment<TItem> __arg1)
        {
        builder.OpenComponent<global::Test.MyComponent<TItem>>(seq);
        builder.AddAttribute(__seq0, "Item", __arg0);
        builder.AddAttribute(__seq1, "ChildContent", __arg1);
        builder.CloseComponent();
        }
    }
}
#pragma warning restore 1591