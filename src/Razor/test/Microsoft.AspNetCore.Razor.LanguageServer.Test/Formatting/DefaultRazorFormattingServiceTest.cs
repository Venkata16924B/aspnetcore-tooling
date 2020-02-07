// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class DefaultRazorFormattingServiceTest : FormattingTestBase
    {
        [Fact]
        public async Task FormatsCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{}
        public interface Bar {
}
}|
",
expected: @"
@functions {
    public class Foo { }
    public interface Bar
    {
    }
}
");
        }

        [Fact]
        public async Task DoesNotFormat_NonCodeBlockDirectives()
        {
            await RunFormattingTestAsync(
input: @"
|@{
var x = ""foo"";
}
<div>
        </div>|
",
expected: @"
@{
var x = ""foo"";
}
<div>
        </div>
");
        }

        [Fact]
        public async Task DoesNotFormatCodeBlockDirectiveWithMarkup()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{
void Method() { <div></div> }
}
}|
",
expected: @"
@functions {
 public class Foo{
void Method() { <div></div> }
}
}
");
        }

        [Fact]
        public async Task OnlyFormatsWithinRange()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{}
        |public interface Bar {
}|
}
",
expected: @"
@functions {
 public class Foo{}
    public interface Bar
    {
    }
}
");
        }

        [Fact]
        public async Task MultipleCodeBlockDirectives()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{}
        public interface Bar {
}
}
Hello World
@functions {
      public class Baz    {
          void Method ( )
          { }
          }
}|
",
expected: @"
@functions {
    public class Foo { }
    public interface Bar
    {
    }
}
Hello World
@functions {
    public class Baz
    {
        void Method()
        { }
    }
}
");
        }

        [Fact]
        public async Task CodeOnTheSameLineAsCodeBlockDirectiveStart()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {public class Foo{
}
}|
",
expected: @"
@functions {
    public class Foo
    {
    }
}
");
        }

        [Fact]
        public async Task CodeOnTheSameLineAsCodeBlockDirectiveEnd()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
public class Foo{
}}|
",
expected: @"
@functions {
    public class Foo
    {
    }
}
");
        }

        [Fact(Skip = "This doesn't work yet")]
        public async Task SingleLineCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {public class Foo{}}|
",
expected: @"
@functions {
    public class Foo { }
}
");
        }

        [Fact]
        public async Task ComplexCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
|@functions{
     public class Foo
            {
                public Foo()
                {
                    var arr = new string[ ] {
""One"", ""two"",
""three""
                    };
                }
                    public int MyProperty { get
                    {
                    return 0 ;
                    } set {} }

void Method(){

}
                    }
}|
",
expected: @"
@functions{
    public class Foo
    {
        public Foo()
        {
            var arr = new string[] {
""One"", ""two"",
""three""
                };
        }
        public int MyProperty
        {
            get
            {
                return 0;
            }
            set { }
        }

        void Method()
        {

        }
    }
}
");
        }
    }
}
